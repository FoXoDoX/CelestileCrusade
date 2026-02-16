namespace My.Scripts.EventBus
{
    public enum GameEvents
    {
        // === Input ===
        MenuButtonPressed,
        RestartButtonPressed,
        SettingsButtonPressed,
        SettingsBackButtonPressed,

        // === Lander Movement ===
        LanderBeforeForce,
        LanderUpForce,
        LanderLeftForce,
        LanderRightForce,

        // === Lander State ===
        LanderStateChanged,
        LanderLanded,

        // === Pickups ===
        CoinPickup,
        EnergyBookPickup,
        EnergyBookParticle,
        CratePickup,
        KeyPickup,
        KeyDelivered,

        // === Crate ===
        RopeWithCrateSpawned,
        RopeWithCrateDestroyed,
        CrateDrop,
        CrateCracked,
        CrateDestroyed,

        // === Game State ===
        GamePaused,
        GameUnpaused,
        ScoreChanged,

        // === Audio ===
        MusicVolumeChanged,

        // === Turret ===
        TurretShoot,

        // === HotZone ===
        HotZoneStateChanged,

        // === Levels ===
        LevelCompleted,

        // === Tutorial ===
        TutorialStarted,
        TutorialCompleted,
    }
}