#!/usr/bin/env python3
"""
Squid Ink-Pulse fair server MVP.

This is intentionally dependency-free: Python standard library + SQLite.
It runs on the fair host PC, exposes a LAN JSON API for Unity clients, and
serves a simple browser leaderboard.
"""

from __future__ import annotations

import argparse
import json
import os
import random
import re
import secrets
import signal
import sqlite3
import string
import sys
import threading
import time
import uuid
from datetime import datetime, timedelta, timezone
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any
from urllib.parse import parse_qs, urlparse


DEFAULT_HOST = "0.0.0.0"
DEFAULT_PORT = 8080
DEFAULT_EVENT_ID = "feria-2026"
DEFAULT_SESSION_TIMEOUT_MINUTES = 15
RECOVERY_CODE_LENGTH = 4
RECOVERY_CODE_ALPHABET = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"

SNAPSHOT_VERSION = 1


def utc_now() -> datetime:
    return datetime.now(timezone.utc)


def utc_now_text() -> str:
    return utc_now().replace(microsecond=0).isoformat().replace("+00:00", "Z")


def parse_utc(value: str | None) -> datetime | None:
    if not value:
        return None

    try:
        return datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError:
        return None


def normalize_nickname(value: str) -> str:
    normalized = re.sub(r"\s+", " ", value.strip()).upper()
    return normalized


def clamp_nickname(value: Any) -> str:
    if not isinstance(value, str):
        return ""

    normalized = re.sub(r"\s+", " ", value.strip())
    return normalized[:24]


def safe_int(value: Any, default: int = 0) -> int:
    try:
        return max(0, int(value))
    except (TypeError, ValueError):
        return default


def as_dict(value: Any) -> dict[str, Any]:
    return value if isinstance(value, dict) else {}


def as_list(value: Any) -> list[Any]:
    return value if isinstance(value, list) else []


def unique_strings(values: list[Any]) -> list[str]:
    result: list[str] = []
    seen: set[str] = set()
    for value in values:
        if not isinstance(value, str):
            continue

        trimmed = value.strip()
        if not trimmed or trimmed in seen:
            continue

        seen.add(trimmed)
        result.append(trimmed)

    return result


def read_json_body(handler: BaseHTTPRequestHandler) -> dict[str, Any]:
    length_text = handler.headers.get("Content-Length", "0")
    try:
        length = max(0, int(length_text))
    except ValueError:
        length = 0

    if length == 0:
        return {}

    raw = handler.rfile.read(length)
    if not raw:
        return {}

    try:
        parsed = json.loads(raw.decode("utf-8"))
    except json.JSONDecodeError as exc:
        raise RequestError(HTTPStatus.BAD_REQUEST, "invalid_json", f"JSON invalido: {exc.msg}") from exc

    if not isinstance(parsed, dict):
        raise RequestError(HTTPStatus.BAD_REQUEST, "invalid_json", "El cuerpo debe ser un objeto JSON.")

    return parsed


class RequestError(Exception):
    def __init__(self, status: HTTPStatus, code: str, message: str, details: dict[str, Any] | None = None):
        super().__init__(message)
        self.status = status
        self.code = code
        self.message = message
        self.details = details or {}

    def to_payload(self) -> dict[str, Any]:
        payload = {
            "ok": False,
            "error": self.code,
            "message": self.message,
        }
        payload.update(self.details)
        return payload


class FairStore:
    def __init__(self, db_path: Path, event_id: str, session_timeout_minutes: int):
        self.db_path = db_path
        self.event_id = event_id
        self.session_timeout = timedelta(minutes=session_timeout_minutes)
        self.lock = threading.RLock()
        self.db_path.parent.mkdir(parents=True, exist_ok=True)
        self._initialize()

    def _connect(self) -> sqlite3.Connection:
        connection = sqlite3.connect(self.db_path, timeout=15, isolation_level=None)
        connection.row_factory = sqlite3.Row
        connection.execute("PRAGMA foreign_keys = ON;")
        connection.execute("PRAGMA journal_mode = WAL;")
        return connection

    def _initialize(self) -> None:
        with self.lock, self._connect() as connection:
            connection.executescript(
                """
                CREATE TABLE IF NOT EXISTS fair_participants (
                    participant_id TEXT PRIMARY KEY,
                    nickname TEXT NOT NULL,
                    recovery_code TEXT NOT NULL,
                    nickname_normalized TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    last_machine_id TEXT,
                    active_session_machine_id TEXT,
                    active_session_expires_at TEXT,
                    checked_out_at TEXT,
                    build_version TEXT,
                    best_score INTEGER NOT NULL DEFAULT 0,
                    attempt_count INTEGER NOT NULL DEFAULT 0,
                    total_shrimps INTEGER NOT NULL DEFAULT 0,
                    total_shrimps_collected INTEGER NOT NULL DEFAULT 0,
                    total_portals_crossed INTEGER NOT NULL DEFAULT 0,
                    snapshot_json TEXT NOT NULL
                );

                CREATE UNIQUE INDEX IF NOT EXISTS idx_fair_recovery
                ON fair_participants(nickname_normalized, recovery_code);

                CREATE INDEX IF NOT EXISTS idx_fair_leaderboard
                ON fair_participants(best_score DESC, updated_at ASC);

                CREATE TABLE IF NOT EXISTS fair_events (
                    event_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    participant_id TEXT,
                    event_type TEXT NOT NULL,
                    machine_id TEXT,
                    created_at TEXT NOT NULL,
                    payload_json TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_fair_events_participant
                ON fair_events(participant_id, created_at);
                """
            )

    def health(self) -> dict[str, Any]:
        with self.lock, self._connect() as connection:
            count = connection.execute("SELECT COUNT(*) FROM fair_participants;").fetchone()[0]

        return {
            "ok": True,
            "serverTime": utc_now_text(),
            "eventId": self.event_id,
            "participantCount": count,
        }

    def create_participant(self, payload: dict[str, Any]) -> dict[str, Any]:
        nickname = clamp_nickname(payload.get("nickname"))
        if not nickname:
            raise RequestError(HTTPStatus.BAD_REQUEST, "invalid_nickname", "nickname es obligatorio.")

        machine_id = self._required_machine_id(payload)
        build_version = str(payload.get("buildVersion") or "")[:64]
        participant_id = str(uuid.uuid4())
        recovery_code = self._generate_recovery_code(nickname)
        now = utc_now_text()
        expires_at = self._new_session_expiry_text()
        snapshot = default_snapshot(nickname)

        with self.lock, self._connect() as connection:
            connection.execute(
                """
                INSERT INTO fair_participants (
                    participant_id, nickname, recovery_code, nickname_normalized,
                    created_at, updated_at, last_machine_id, active_session_machine_id,
                    active_session_expires_at, build_version, best_score, attempt_count,
                    total_shrimps, total_shrimps_collected, total_portals_crossed, snapshot_json
                )
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 0, 0, 0, 0, 0, ?);
                """,
                (
                    participant_id,
                    nickname,
                    recovery_code,
                    normalize_nickname(nickname),
                    now,
                    now,
                    machine_id,
                    machine_id,
                    expires_at,
                    build_version,
                    json.dumps(snapshot, ensure_ascii=False, separators=(",", ":")),
                ),
            )
            self._log_event(connection, participant_id, "created", machine_id, {"nickname": nickname})

        response = self._participant_response(participant_id, include_recovery_code=True)
        return response

    def recover_participant(self, payload: dict[str, Any]) -> dict[str, Any]:
        nickname = clamp_nickname(payload.get("nickname"))
        recovery_code = str(payload.get("recoveryCode") or "").strip().upper()
        machine_id = self._required_machine_id(payload)
        build_version = str(payload.get("buildVersion") or "")[:64]

        if not nickname or not recovery_code:
            raise RequestError(
                HTTPStatus.BAD_REQUEST,
                "invalid_recovery_request",
                "nickname y recoveryCode son obligatorios.",
            )

        with self.lock, self._connect() as connection:
            row = connection.execute(
                """
                SELECT * FROM fair_participants
                WHERE nickname_normalized = ? AND recovery_code = ?;
                """,
                (normalize_nickname(nickname), recovery_code),
            ).fetchone()

            if row is None:
                raise RequestError(HTTPStatus.NOT_FOUND, "participant_not_found", "Participante no encontrado.")

            self._assert_session_available(row, machine_id)
            now = utc_now_text()
            expires_at = self._new_session_expiry_text()
            connection.execute(
                """
                UPDATE fair_participants
                SET updated_at = ?, last_machine_id = ?, active_session_machine_id = ?,
                    active_session_expires_at = ?, checked_out_at = NULL,
                    build_version = COALESCE(NULLIF(?, ''), build_version)
                WHERE participant_id = ?;
                """,
                (now, machine_id, machine_id, expires_at, build_version, row["participant_id"]),
            )
            self._log_event(connection, row["participant_id"], "recovered", machine_id, {})

        return self._participant_response(row["participant_id"], include_recovery_code=False)

    def get_participant(self, participant_id: str) -> dict[str, Any]:
        return self._participant_response(participant_id, include_recovery_code=False)

    def put_snapshot(self, participant_id: str, payload: dict[str, Any]) -> dict[str, Any]:
        machine_id = self._required_machine_id(payload)
        incoming = payload.get("snapshot")
        if not isinstance(incoming, dict):
            incoming = payload

        with self.lock, self._connect() as connection:
            row = self._require_participant(connection, participant_id)
            self._assert_session_available(row, machine_id)

            current_snapshot = self._snapshot_from_row(row)
            merged_snapshot, stats = merge_snapshot(current_snapshot, incoming, nickname=row["nickname"])
            now = utc_now_text()
            expires_at = self._new_session_expiry_text()
            build_version = str(payload.get("buildVersion") or incoming.get("buildVersion") or row["build_version"] or "")[:64]

            connection.execute(
                """
                UPDATE fair_participants
                SET updated_at = ?, last_machine_id = ?, active_session_machine_id = ?,
                    active_session_expires_at = ?, build_version = ?, best_score = ?,
                    attempt_count = ?, total_shrimps = ?, total_shrimps_collected = ?,
                    total_portals_crossed = ?, snapshot_json = ?
                WHERE participant_id = ?;
                """,
                (
                    now,
                    machine_id,
                    machine_id,
                    expires_at,
                    build_version,
                    stats["bestScore"],
                    stats["attemptCount"],
                    stats["totalShrimps"],
                    stats["totalShrimpsCollected"],
                    stats["totalPortalsCrossed"],
                    json.dumps(merged_snapshot, ensure_ascii=False, separators=(",", ":")),
                    participant_id,
                ),
            )
            self._log_event(
                connection,
                participant_id,
                "snapshot_synced",
                machine_id,
                {
                    "bestScore": stats["bestScore"],
                    "attemptCount": stats["attemptCount"],
                },
            )

        rank = self.rank(participant_id)
        return {
            "accepted": True,
            "rank": rank["rank"],
            "bestScore": rank["bestScore"],
            "leaderboardCount": rank["leaderboardCount"],
            "profileSnapshot": merged_snapshot,
        }

    def heartbeat(self, participant_id: str, payload: dict[str, Any]) -> dict[str, Any]:
        machine_id = self._required_machine_id(payload)
        with self.lock, self._connect() as connection:
            row = self._require_participant(connection, participant_id)
            self._assert_session_available(row, machine_id)
            expires_at = self._new_session_expiry_text()
            connection.execute(
                """
                UPDATE fair_participants
                SET active_session_machine_id = ?, active_session_expires_at = ?,
                    last_machine_id = ?, updated_at = ?
                WHERE participant_id = ?;
                """,
                (machine_id, expires_at, machine_id, utc_now_text(), participant_id),
            )
            self._log_event(connection, participant_id, "heartbeat", machine_id, {})

        return {
            "accepted": True,
            "activeSessionMachineId": machine_id,
            "activeSessionExpiresAt": expires_at,
        }

    def checkout(self, participant_id: str, payload: dict[str, Any]) -> dict[str, Any]:
        machine_id = self._required_machine_id(payload)
        final_snapshot = payload.get("finalSnapshot")
        if isinstance(final_snapshot, dict):
            sync_payload = dict(final_snapshot)
            sync_payload["machineId"] = machine_id
            self.put_snapshot(participant_id, sync_payload)

        with self.lock, self._connect() as connection:
            row = self._require_participant(connection, participant_id)
            self._assert_session_available(row, machine_id)
            now = utc_now_text()
            connection.execute(
                """
                UPDATE fair_participants
                SET updated_at = ?, checked_out_at = ?, active_session_machine_id = NULL,
                    active_session_expires_at = NULL, last_machine_id = ?
                WHERE participant_id = ?;
                """,
                (now, now, machine_id, participant_id),
            )
            self._log_event(connection, participant_id, "checkout", machine_id, {})

        rank = self.rank(participant_id)
        return {
            "accepted": True,
            "rank": rank["rank"],
            "leaderboardCount": rank["leaderboardCount"],
            "bestScore": rank["bestScore"],
        }

    def rank(self, participant_id: str) -> dict[str, Any]:
        entries = self.leaderboard(limit=100000)["entries"]
        for entry in entries:
            if entry["participantId"] == participant_id:
                return {
                    "rank": entry["rank"],
                    "leaderboardCount": len(entries),
                    "bestScore": entry["bestScore"],
                }

        raise RequestError(HTTPStatus.NOT_FOUND, "participant_not_found", "Participante no encontrado.")

    def leaderboard(self, limit: int = 20) -> dict[str, Any]:
        safe_limit = min(max(limit, 1), 100000)
        with self.lock, self._connect() as connection:
            rows = connection.execute(
                """
                SELECT participant_id, nickname, best_score, attempt_count,
                       total_shrimps_collected, total_portals_crossed,
                       checked_out_at, updated_at
                FROM fair_participants
                ORDER BY best_score DESC, total_shrimps_collected DESC,
                         attempt_count ASC, updated_at ASC, nickname ASC;
                """
            ).fetchall()

        entries = []
        for rank_index, row in enumerate(rows, start=1):
            if len(entries) >= safe_limit:
                break

            entries.append(
                {
                    "rank": rank_index,
                    "participantId": row["participant_id"],
                    "nickname": row["nickname"],
                    "bestScore": row["best_score"],
                    "attemptCount": row["attempt_count"],
                    "totalShrimpsCollected": row["total_shrimps_collected"],
                    "totalPortalsCrossed": row["total_portals_crossed"],
                    "checkedOut": row["checked_out_at"] is not None,
                    "updatedAt": row["updated_at"],
                }
            )

        return {
            "entries": entries,
            "leaderboardCount": len(rows),
            "serverTime": utc_now_text(),
            "eventId": self.event_id,
        }

    def _participant_response(self, participant_id: str, include_recovery_code: bool) -> dict[str, Any]:
        with self.lock, self._connect() as connection:
            row = self._require_participant(connection, participant_id)

        snapshot = self._snapshot_from_row(row)
        response = {
            "participantId": row["participant_id"],
            "nickname": row["nickname"],
            "profileSnapshot": snapshot,
            "bestScore": row["best_score"],
            "attemptCount": row["attempt_count"],
            "activeSessionMachineId": row["active_session_machine_id"],
            "activeSessionExpiresAt": row["active_session_expires_at"],
        }
        if include_recovery_code:
            response["recoveryCode"] = row["recovery_code"]

        return response

    def _required_machine_id(self, payload: dict[str, Any]) -> str:
        machine_id = str(payload.get("machineId") or "").strip()[:64]
        if not machine_id:
            raise RequestError(HTTPStatus.BAD_REQUEST, "invalid_machine_id", "machineId es obligatorio.")

        return machine_id

    def _generate_recovery_code(self, nickname: str) -> str:
        normalized = normalize_nickname(nickname)
        with self.lock, self._connect() as connection:
            for _ in range(100):
                code = "".join(secrets.choice(RECOVERY_CODE_ALPHABET) for _ in range(RECOVERY_CODE_LENGTH))
                exists = connection.execute(
                    """
                    SELECT 1 FROM fair_participants
                    WHERE nickname_normalized = ? AND recovery_code = ?;
                    """,
                    (normalized, code),
                ).fetchone()
                if exists is None:
                    return code

        return f"{random.randint(1000, 9999)}"

    def _new_session_expiry_text(self) -> str:
        expires_at = utc_now() + self.session_timeout
        return expires_at.replace(microsecond=0).isoformat().replace("+00:00", "Z")

    def _assert_session_available(self, row: sqlite3.Row, machine_id: str) -> None:
        active_machine = row["active_session_machine_id"]
        expires_at = parse_utc(row["active_session_expires_at"])
        if not active_machine or active_machine == machine_id:
            return

        if expires_at is not None and expires_at <= utc_now():
            return

        raise RequestError(
            HTTPStatus.CONFLICT,
            "active_session",
            "El participante ya esta activo en otro PC.",
            {
                "activeSessionMachineId": active_machine,
                "activeSessionExpiresAt": row["active_session_expires_at"],
            },
        )

    def _require_participant(self, connection: sqlite3.Connection, participant_id: str) -> sqlite3.Row:
        row = connection.execute(
            "SELECT * FROM fair_participants WHERE participant_id = ?;",
            (participant_id,),
        ).fetchone()
        if row is None:
            raise RequestError(HTTPStatus.NOT_FOUND, "participant_not_found", "Participante no encontrado.")

        return row

    def _snapshot_from_row(self, row: sqlite3.Row) -> dict[str, Any]:
        try:
            snapshot = json.loads(row["snapshot_json"])
        except (TypeError, json.JSONDecodeError):
            snapshot = default_snapshot(row["nickname"])

        return as_dict(snapshot)

    def _log_event(
        self,
        connection: sqlite3.Connection,
        participant_id: str | None,
        event_type: str,
        machine_id: str | None,
        payload: dict[str, Any],
    ) -> None:
        connection.execute(
            """
            INSERT INTO fair_events (participant_id, event_type, machine_id, created_at, payload_json)
            VALUES (?, ?, ?, ?, ?);
            """,
            (
                participant_id,
                event_type,
                machine_id,
                utc_now_text(),
                json.dumps(payload, ensure_ascii=False, separators=(",", ":")),
            ),
        )


def default_snapshot(nickname: str) -> dict[str, Any]:
    return {
        "version": SNAPSHOT_VERSION,
        "nickname": nickname,
        "records": {
            "bestScore": 0,
            "totalRuns": 0,
            "totalShrimps": 0,
            "totalShrimpsCollected": 0,
            "totalPortalsCrossed": 0,
        },
        "profile": {
            "permanentUpgrades": {
                "inkPulseDurationLevel": 0,
                "inkPulseRechargeRateLevel": 0,
                "shrimpMultiplierLevel": 0,
                "scoreMultiplierLevel": 0,
            },
            "skins": {
                "unlockedSkinIds": ["skin.default"],
                "equippedSkinId": "skin.default",
            },
            "runGadgetUnlocks": {
                "unlockedRunGadgetIds": ["gadget.shell_shield", "gadget.ink_bottle"],
            },
        },
        "unlockedEvents": [],
        "updatedAt": utc_now_text(),
    }


def merge_snapshot(current: dict[str, Any], incoming: dict[str, Any], nickname: str) -> tuple[dict[str, Any], dict[str, int]]:
    base = default_snapshot(nickname)
    current_records = as_dict(current.get("records"))
    incoming_records = as_dict(incoming.get("records"))

    best_score = max(
        safe_int(current.get("bestScore")),
        safe_int(current_records.get("bestScore")),
        safe_int(incoming.get("bestScore")),
        safe_int(incoming_records.get("bestScore")),
    )
    attempt_count = max(
        safe_int(current.get("attemptCount")),
        safe_int(current_records.get("totalRuns")),
        safe_int(incoming.get("attemptCount")),
        safe_int(incoming.get("totalRuns")),
        safe_int(incoming_records.get("totalRuns")),
    )
    total_shrimps = first_non_negative_int(
        incoming.get("totalShrimps"),
        incoming_records.get("totalShrimps"),
        current_records.get("totalShrimps"),
        current.get("totalShrimps"),
    )
    total_shrimps_collected = max(
        safe_int(current_records.get("totalShrimpsCollected")),
        safe_int(current.get("totalShrimpsCollected")),
        safe_int(incoming.get("totalShrimpsCollected")),
        safe_int(incoming_records.get("totalShrimpsCollected")),
    )
    total_portals_crossed = max(
        safe_int(current_records.get("totalPortalsCrossed")),
        safe_int(current.get("totalPortalsCrossed")),
        safe_int(incoming.get("totalPortalsCrossed")),
        safe_int(incoming_records.get("totalPortalsCrossed")),
    )

    current_profile = as_dict(current.get("profile"))
    incoming_profile = as_dict(incoming.get("profile"))
    merged_upgrades = merge_numeric_map(
        as_dict(as_dict(current_profile.get("permanentUpgrades"))),
        as_dict(as_dict(incoming_profile.get("permanentUpgrades"))),
    )
    if not merged_upgrades:
        merged_upgrades = base["profile"]["permanentUpgrades"]

    current_skins = as_dict(current_profile.get("skins"))
    incoming_skins = as_dict(incoming_profile.get("skins"))
    unlocked_skins = unique_strings(
        as_list(current_skins.get("unlockedSkinIds"))
        + as_list(incoming_skins.get("unlockedSkinIds"))
        + ["skin.default"]
    )
    equipped_skin = (
        incoming_skins.get("equippedSkinId")
        if isinstance(incoming_skins.get("equippedSkinId"), str)
        else current_skins.get("equippedSkinId")
    )
    if not isinstance(equipped_skin, str) or equipped_skin not in unlocked_skins:
        equipped_skin = "skin.default"

    current_gadgets = as_dict(current_profile.get("runGadgetUnlocks"))
    incoming_gadgets = as_dict(incoming_profile.get("runGadgetUnlocks"))
    unlocked_gadgets = unique_strings(
        as_list(current_gadgets.get("unlockedRunGadgetIds"))
        + as_list(incoming_gadgets.get("unlockedRunGadgetIds"))
        + base["profile"]["runGadgetUnlocks"]["unlockedRunGadgetIds"]
    )

    unlocked_events = unique_strings(
        as_list(current.get("unlockedEvents"))
        + as_list(incoming.get("unlockedEvents"))
    )

    merged = {
        "version": SNAPSHOT_VERSION,
        "nickname": nickname,
        "records": {
            "bestScore": best_score,
            "totalRuns": attempt_count,
            "totalShrimps": total_shrimps,
            "totalShrimpsCollected": total_shrimps_collected,
            "totalPortalsCrossed": total_portals_crossed,
        },
        "profile": {
            "permanentUpgrades": merged_upgrades,
            "skins": {
                "unlockedSkinIds": unlocked_skins,
                "equippedSkinId": equipped_skin,
            },
            "runGadgetUnlocks": {
                "unlockedRunGadgetIds": unlocked_gadgets,
            },
        },
        "unlockedEvents": unlocked_events,
        "updatedAt": utc_now_text(),
    }

    stats = {
        "bestScore": best_score,
        "attemptCount": attempt_count,
        "totalShrimps": total_shrimps,
        "totalShrimpsCollected": total_shrimps_collected,
        "totalPortalsCrossed": total_portals_crossed,
    }
    return merged, stats


def first_non_negative_int(*values: Any) -> int:
    for value in values:
        try:
            number = int(value)
        except (TypeError, ValueError):
            continue

        if number >= 0:
            return number

    return 0


def merge_numeric_map(current: dict[str, Any], incoming: dict[str, Any]) -> dict[str, int]:
    keys = set(current.keys()) | set(incoming.keys())
    result: dict[str, int] = {}
    for key in sorted(keys):
        if not isinstance(key, str):
            continue

        result[key] = max(safe_int(current.get(key)), safe_int(incoming.get(key)))

    return result


LEADERBOARD_HTML = """<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Squid Ink-Pulse Feria</title>
  <style>
    :root {
      color-scheme: dark;
      font-family: Arial, Helvetica, sans-serif;
      background: #061923;
      color: #f5fbff;
    }
    body {
      margin: 0;
      min-height: 100vh;
      background:
        radial-gradient(circle at 20% 20%, rgba(22, 164, 204, 0.25), transparent 34rem),
        linear-gradient(135deg, #061923, #123244 45%, #09141b);
    }
    main {
      max-width: 1180px;
      margin: 0 auto;
      padding: 32px;
    }
    header {
      display: flex;
      justify-content: space-between;
      align-items: end;
      gap: 24px;
      margin-bottom: 28px;
    }
    h1 {
      margin: 0;
      font-size: 44px;
      letter-spacing: 0;
      line-height: 1;
    }
    .meta {
      text-align: right;
      color: #bde8f6;
      font-size: 16px;
    }
    table {
      width: 100%;
      border-collapse: collapse;
      background: rgba(2, 10, 14, 0.72);
      border: 1px solid rgba(171, 235, 255, 0.2);
    }
    th, td {
      padding: 18px 20px;
      border-bottom: 1px solid rgba(171, 235, 255, 0.12);
      text-align: left;
      font-size: 22px;
    }
    th {
      color: #7be4ff;
      font-size: 15px;
      text-transform: uppercase;
      letter-spacing: 0;
    }
    .rank {
      width: 90px;
      color: #ffd769;
      font-weight: 800;
      font-size: 30px;
    }
    .score {
      font-weight: 800;
      font-size: 30px;
    }
    .muted {
      color: #9fc7d2;
    }
    .empty {
      padding: 48px 20px;
      text-align: center;
      color: #bde8f6;
      font-size: 22px;
    }
  </style>
</head>
<body>
  <main>
    <header>
      <div>
        <h1>Squid Ink-Pulse</h1>
        <div class="muted">Ranking de feria</div>
      </div>
      <div class="meta">
        <div id="event">Evento</div>
        <div id="time">Actualizando...</div>
      </div>
    </header>
    <table>
      <thead>
        <tr>
          <th>#</th>
          <th>Jugador</th>
          <th>Puntaje</th>
          <th>Intentos</th>
          <th>Camarones</th>
        </tr>
      </thead>
      <tbody id="rows">
        <tr><td class="empty" colspan="5">Esperando participantes...</td></tr>
      </tbody>
    </table>
  </main>
  <script>
    function formatNumber(value) {
      return new Intl.NumberFormat("es-CL").format(value || 0);
    }
    async function refresh() {
      try {
        const response = await fetch("/leaderboard?limit=20", { cache: "no-store" });
        const data = await response.json();
        document.getElementById("event").textContent = data.eventId || "feria";
        document.getElementById("time").textContent = new Date(data.serverTime).toLocaleTimeString("es-CL");
        const rows = document.getElementById("rows");
        if (!data.entries || data.entries.length === 0) {
          rows.innerHTML = '<tr><td class="empty" colspan="5">Esperando participantes...</td></tr>';
          return;
        }
        rows.innerHTML = data.entries.map(entry => `
          <tr>
            <td class="rank">${entry.rank}</td>
            <td>${entry.nickname}</td>
            <td class="score">${formatNumber(entry.bestScore)}</td>
            <td>${formatNumber(entry.attemptCount)}</td>
            <td>${formatNumber(entry.totalShrimpsCollected)}</td>
          </tr>
        `).join("");
      } catch (error) {
        document.getElementById("time").textContent = "Sin conexion";
      }
    }
    refresh();
    setInterval(refresh, 5000);
  </script>
</body>
</html>
"""


class FairRequestHandler(BaseHTTPRequestHandler):
    server_version = "SquidFairServer/1.0"

    @property
    def store(self) -> FairStore:
        return self.server.store  # type: ignore[attr-defined]

    def do_OPTIONS(self) -> None:
        self.send_response(HTTPStatus.NO_CONTENT)
        self._send_cors_headers()
        self.end_headers()

    def do_GET(self) -> None:
        self._handle("GET")

    def do_POST(self) -> None:
        self._handle("POST")

    def do_PUT(self) -> None:
        self._handle("PUT")

    def log_message(self, format_text: str, *args: Any) -> None:
        sys.stdout.write(f"[{utc_now_text()}] {self.address_string()} {format_text % args}\n")

    def _handle(self, method: str) -> None:
        try:
            parsed = urlparse(self.path)
            path = parsed.path.rstrip("/") or "/"
            query = parse_qs(parsed.query)

            if method == "GET" and path in {"/", "/leaderboard.html"}:
                self._send_html(LEADERBOARD_HTML)
                return

            if method == "GET" and path == "/health":
                self._send_json(self.store.health())
                return

            if method == "GET" and path == "/leaderboard":
                limit = safe_int(first_query_value(query, "limit"), 20)
                self._send_json(self.store.leaderboard(limit=limit))
                return

            if method == "POST" and path == "/participants":
                self._send_json(self.store.create_participant(read_json_body(self)), status=HTTPStatus.CREATED)
                return

            if method == "POST" and path == "/participants/recover":
                self._send_json(self.store.recover_participant(read_json_body(self)))
                return

            participant_match = re.fullmatch(r"/participants/([^/]+)(?:/(snapshot|rank|checkout|heartbeat))?", path)
            if participant_match:
                participant_id = participant_match.group(1)
                action = participant_match.group(2)
                if method == "GET" and action is None:
                    self._send_json(self.store.get_participant(participant_id))
                    return

                if method == "PUT" and action == "snapshot":
                    self._send_json(self.store.put_snapshot(participant_id, read_json_body(self)))
                    return

                if method == "GET" and action == "rank":
                    self._send_json(self.store.rank(participant_id))
                    return

                if method == "POST" and action == "checkout":
                    self._send_json(self.store.checkout(participant_id, read_json_body(self)))
                    return

                if method == "POST" and action == "heartbeat":
                    self._send_json(self.store.heartbeat(participant_id, read_json_body(self)))
                    return

            raise RequestError(HTTPStatus.NOT_FOUND, "not_found", "Endpoint no encontrado.")
        except RequestError as exc:
            self._send_json(exc.to_payload(), status=exc.status)
        except Exception as exc:  # noqa: BLE001 - final HTTP boundary.
            self._send_json(
                {
                    "ok": False,
                    "error": "internal_error",
                    "message": str(exc),
                },
                status=HTTPStatus.INTERNAL_SERVER_ERROR,
            )

    def _send_json(self, payload: dict[str, Any], status: HTTPStatus = HTTPStatus.OK) -> None:
        raw = json.dumps(payload, ensure_ascii=False, indent=2).encode("utf-8")
        self.send_response(status)
        self._send_cors_headers()
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(raw)))
        self.end_headers()
        self.wfile.write(raw)

    def _send_html(self, html: str) -> None:
        raw = html.encode("utf-8")
        self.send_response(HTTPStatus.OK)
        self._send_cors_headers()
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Cache-Control", "no-store")
        self.send_header("Content-Length", str(len(raw)))
        self.end_headers()
        self.wfile.write(raw)

    def _send_cors_headers(self) -> None:
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, PUT, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")


def first_query_value(query: dict[str, list[str]], key: str) -> str | None:
    values = query.get(key)
    return values[0] if values else None


class FairHttpServer(ThreadingHTTPServer):
    def __init__(self, server_address: tuple[str, int], handler_class: type[BaseHTTPRequestHandler], store: FairStore):
        super().__init__(server_address, handler_class)
        self.store = store


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Squid Ink-Pulse fair server MVP.")
    parser.add_argument("--host", default=DEFAULT_HOST, help="Host/IP to bind. Use 0.0.0.0 for LAN.")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT, help="HTTP port.")
    parser.add_argument(
        "--db",
        default=str(Path(__file__).with_name("data") / "fair_server.sqlite3"),
        help="SQLite database path.",
    )
    parser.add_argument("--event-id", default=DEFAULT_EVENT_ID, help="Event identifier returned by /health.")
    parser.add_argument(
        "--session-timeout-minutes",
        type=int,
        default=DEFAULT_SESSION_TIMEOUT_MINUTES,
        help="Soft exclusive-session timeout.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    db_path = Path(args.db).resolve()
    store = FairStore(
        db_path=db_path,
        event_id=args.event_id,
        session_timeout_minutes=max(1, args.session_timeout_minutes),
    )
    server = FairHttpServer((args.host, args.port), FairRequestHandler, store)

    def stop_server(signum: int, frame: Any) -> None:  # noqa: ARG001
        print("\nStopping fair server...")
        server.shutdown()

    if hasattr(signal, "SIGINT"):
        signal.signal(signal.SIGINT, stop_server)
    if hasattr(signal, "SIGTERM"):
        signal.signal(signal.SIGTERM, stop_server)

    print("Squid Ink-Pulse fair server")
    print(f"Event: {args.event_id}")
    print(f"Database: {db_path}")
    print(f"Listen: http://{args.host}:{args.port}")
    print(f"Leaderboard: http://localhost:{args.port}/")
    print("Press Ctrl+C to stop.")
    server.serve_forever()
    server.server_close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
