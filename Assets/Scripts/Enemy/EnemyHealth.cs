using UnityEngine;
using System.Collections;


public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health of this enemy.")]
    public float maxHealth = 20f;
    private float _currentHealth;

    [Header("XP Settings")]
    [Tooltip("Amount of XP rewarded to the player when this enemy dies.")]
    public float xpReward = 15f;

    [Header("Gold Settings")]
    [Tooltip("Amount of Gold rewarded to the player when this enemy dies.")]
    public int goldReward = 5;

    [Header("Hit Flash")]
    [Tooltip("Color the enemy briefly tints to when it takes damage.")]
    public Color flashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [Tooltip("How long the hit flash lasts, in seconds.")]
    public float flashDuration = 0.08f;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;
    private Coroutine _flashRoutine;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");


private void Start()
    {
        _currentHealth = maxHealth;

        // Cache renderers + one shared property block for the hit-flash effect.
        // MaterialPropertyBlock tints per-instance without cloning materials (no leaks).
        _renderers = GetComponentsInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

public void TakeDamage(float damage)
    {
        _currentHealth -= damage;

        // Floating damage number pops on every hit.
        JuiceManager.DamageNumber(transform.position, damage);

        if (_currentHealth <= 0)
        {
            Die();
            return;
        }

        // Surviving hit: tiny freeze + small shake so hits feel weighty.
        JuiceManager.HitStop(0.03f);
        JuiceManager.Shake(0.12f);

        // Flash on hit (only if the enemy survives; dead ones are destroyed immediately).
        if (_renderers != null && _renderers.Length > 0)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }
    }

    private void Die()
    {
        // Debris burst + a bigger shake on death. (No hit-stop here: a kill can
        // trigger a level-up that pauses the game, and we don't want to fight it.)
        JuiceManager.DeathPop(transform.position, GetEnemyColor());
        JuiceManager.Shake(0.3f);

        // Award XP and Gold to the player stats when the enemy dies
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerStats stats = playerObj.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.AddXP(xpReward);
                stats.AddGold(goldReward);
            }
        }

        // Destroy the enemy GameObject
        Destroy(gameObject);
    }


// Reads the enemy's main color so the death debris matches it. Falls back to white.
    private Color GetEnemyColor()
    {
        if (_renderers != null)
        {
            foreach (Renderer r in _renderers)
            {
                if (r == null || r.sharedMaterial == null) continue;
                if (r.sharedMaterial.HasProperty(BaseColorId)) return r.sharedMaterial.GetColor(BaseColorId);
                if (r.sharedMaterial.HasProperty(ColorId)) return r.sharedMaterial.GetColor(ColorId);
            }
        }
        return Color.white;
    }

    // Briefly tints all renderers to flashColor, then clears the override.
    private IEnumerator FlashRoutine()
    {
        foreach (Renderer r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, flashColor); // URP Lit
            _mpb.SetColor(ColorId, flashColor);     // Standard / legacy shaders
            r.SetPropertyBlock(_mpb);
        }

        yield return new WaitForSeconds(flashDuration);

        // Clearing the block returns each renderer to its original material color.
        foreach (Renderer r in _renderers)
        {
            if (r != null) r.SetPropertyBlock(null);
        }
        _flashRoutine = null;
    }
}
