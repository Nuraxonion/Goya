using UnityEngine;

// Persistent coin currency, saved between runs in PlayerPrefs.
// Static so it can be read/written from any scene without a singleton
// or Inspector wiring.
public static class CoinBank
{
    private const string COINS_KEY = "Coins";

    public static int GetCoins()
    {
        return PlayerPrefs.GetInt(COINS_KEY, 0);
    }

    public static void AddCoins(int amount)
    {
        if (amount <= 0) return;

        int total = GetCoins() + amount;
        PlayerPrefs.SetInt(COINS_KEY, total);
        PlayerPrefs.Save();
    }

    // Returns true if the player could afford it and the coins were spent.
    public static bool SpendCoins(int amount)
    {
        int current = GetCoins();

        if (amount <= 0 || current < amount) return false;

        PlayerPrefs.SetInt(COINS_KEY, current - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static bool HasCoins(int amount)
    {
        return GetCoins() >= amount;
    }
}
