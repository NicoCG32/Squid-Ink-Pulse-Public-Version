#!/usr/bin/env python3
from __future__ import annotations

import json
import urllib.request


BASE_URL = "http://127.0.0.1:8080"


def request(method: str, path: str, payload: dict | None = None) -> dict:
    body = None
    headers = {"Content-Type": "application/json"}
    if payload is not None:
        body = json.dumps(payload).encode("utf-8")

    req = urllib.request.Request(BASE_URL + path, data=body, headers=headers, method=method)
    with urllib.request.urlopen(req, timeout=5) as response:
        return json.loads(response.read().decode("utf-8"))


def main() -> None:
    health = request("GET", "/health")
    assert health["ok"] is True

    created = request(
        "POST",
        "/participants",
        {
            "nickname": "NICO",
            "machineId": "PC-TEST",
            "buildVersion": "smoke-test",
        },
    )
    participant_id = created["participantId"]
    assert created["recoveryCode"]

    synced = request(
        "PUT",
        f"/participants/{participant_id}/snapshot",
        {
            "machineId": "PC-TEST",
            "bestScore": 123456,
            "attemptCount": 3,
            "records": {
                "totalShrimps": 777,
                "totalShrimpsCollected": 1000,
                "totalPortalsCrossed": 2,
            },
            "profile": {
                "permanentUpgrades": {
                    "inkPulseDurationLevel": 2,
                    "scoreMultiplierLevel": 1,
                },
                "skins": {
                    "unlockedSkinIds": ["skin.default", "skin.sonic"],
                    "equippedSkinId": "skin.sonic",
                },
            },
        },
    )
    assert synced["accepted"] is True
    assert synced["bestScore"] == 123456

    rank = request("GET", f"/participants/{participant_id}/rank")
    assert rank["rank"] >= 1

    leaderboard = request("GET", "/leaderboard?limit=10")
    assert leaderboard["entries"]

    checkout = request("POST", f"/participants/{participant_id}/checkout", {"machineId": "PC-TEST"})
    assert checkout["accepted"] is True

    print("Smoke test OK")


if __name__ == "__main__":
    main()
