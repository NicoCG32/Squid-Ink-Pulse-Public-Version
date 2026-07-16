using System;

public static class LoreComicEntrySelector
{
    public static LoreComicEntry FindEntry(LoreComicEntry[] entries, LoreComicRequest request)
    {
        if (entries == null)
        {
            return null;
        }

        LoreComicEntry fallback = null;
        for (int i = 0; i < entries.Length; i++)
        {
            LoreComicEntry entry = entries[i];
            if (entry == null || entry.comicEvent != request.ComicEvent)
            {
                continue;
            }

            if (entry.zone == request.Zone)
            {
                return entry;
            }

            if (entry.zone == LoreComicZone.Unknown)
            {
                fallback = entry;
            }
        }

        return fallback;
    }

    public static bool HasDisplayableSprite(LoreComicEntry entry)
    {
        if (entry?.sprites == null)
        {
            return false;
        }

        for (int i = 0; i < entry.sprites.Length; i++)
        {
            if (entry.sprites[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    public static float ResolveDisplaySeconds(LoreComicEntry entry, LoreComicRequest request, float defaultDisplaySeconds, float defaultStartDisplaySeconds)
    {
        if (entry != null)
        {
            return Math.Max(0f, entry.displaySeconds);
        }

        return request.ComicEvent == LoreComicEvent.GameStart
            ? Math.Max(0f, defaultStartDisplaySeconds)
            : Math.Max(0f, defaultDisplaySeconds);
    }

    public static bool ResolveWaitForContinue(LoreComicEntry entry, LoreComicRequest request, bool defaultStartWaitsForContinue)
    {
        if (entry != null)
        {
            return entry.waitForContinue;
        }

        return request.ComicEvent == LoreComicEvent.GameStart && defaultStartWaitsForContinue;
    }

    public static bool ResolveShowContinueButton(LoreComicEntry entry, LoreComicRequest request, bool defaultStartShowsContinueButton)
    {
        if (entry != null)
        {
            return entry.showContinueButton;
        }

        return request.ComicEvent == LoreComicEvent.GameStart && defaultStartShowsContinueButton;
    }
}
