﻿using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper component to handle stat management of a unit (health, damage calculation etc.)
/// </summary>
public class UnitStatManagement : MonoBehaviour
{
    [SerializeField]
    private Canvas m_healthBarCanvas;

    [SerializeField]
    private Image m_damageTakenBar;

    [SerializeField]
    private Image m_healthLeftBar;

    private int m_currentHealth;
    public int CurrentHealth { get { return m_currentHealth; } }

    private int m_maxHealth;
    private BaseUnit m_baseUnit;

    /// <summary>
    /// Initializes stat management with the base stat values.
    /// </summary>
    /// <param name="baseUnit">The base unit.</param>
    /// <param name="maxHealth">The maximum health.</param>
    public void Initialize(BaseUnit baseUnit, int maxHealth)
    {
        m_baseUnit = baseUnit;

        m_maxHealth = maxHealth;
        m_currentHealth = maxHealth;

        m_healthBarCanvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Takes the damage.
    /// </summary>
    /// <param name="damage">The damage.</param>
    public void TakeDamage(int damage)
    {
        m_currentHealth = Mathf.Clamp(m_currentHealth - damage, 0, m_maxHealth);

        if (m_currentHealth < m_maxHealth)
        {
            m_healthBarCanvas.gameObject.SetActive(true);

            float normalizedHealthLeft = (float)m_currentHealth / m_maxHealth;

            m_healthLeftBar.fillAmount = normalizedHealthLeft;
            m_damageTakenBar.fillAmount = 1 - normalizedHealthLeft;
        }

        if (m_currentHealth == 0)
        {
            m_baseUnit.Die();
        }
    }

    /// <summary>
    /// Gets the health based damage modifier.
    /// </summary>
    /// <returns></returns>
    public float GetHealthBasedDamageModifier()
    {
        return (float) m_currentHealth / m_maxHealth;
    }
}
