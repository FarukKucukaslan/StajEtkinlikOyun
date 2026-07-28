ICONS GO HERE
=============

Drop PNG (or SVG-exported PNG) icon files into this folder. They are auto-imported
as UI Sprites (handled by Assets/Scripts/Editor/IconAutoImporter.cs), and the UI
picks them up automatically at runtime — no dragging into the Inspector.

FILENAMES MUST MATCH THESE KEYS EXACTLY (case-sensitive), each a .png:

  Level-up + Shop stats (used by both screens):
    MaxHealth.png          -> health / vitality
    MoveSpeed.png          -> movement speed
    AttackSpeed.png        -> attack cooldown
    SwordDamage.png        -> sword damage
    SwordCount.png         -> extra swords
    PierceCount.png        -> pierce
    Defense.png            -> armor
    SearchRange.png        -> target range
    ProjectileSpeed.png    -> projectile speed
    FirstAid.png           -> heal (level-up only)

Optional:
    Gold.png               -> coin icon (not wired yet)

WHERE TO GET THEM (free, consistent set):
  - https://game-icons.net  (thousands of white-silhouette icons, CC BY 3.0 / free)
    Download each as PNG (white on transparent works great with our dark theme),
    rename to the keys above, drop them here. Done.

Any missing icon just shows no image — the game still runs.
