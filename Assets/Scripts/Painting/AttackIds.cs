// Canonical attack identifiers shared by the gesture-to-attack mapping data,
// the gesture recognizer (GestureManager) and the attack spawner (PainterAttack).
// Keeping them here avoids magic strings drifting apart across systems.
public static class AttackIds
{
    public const string None = "";
    public const string Fireball = "Fireball";
    public const string Wave = "Wave";

    // Reserved for future expansion (mapped to the spiral / butterfly gestures):
    // public const string Spiral = "Spiral";
    // public const string Butterfly = "Butterfly";
}
