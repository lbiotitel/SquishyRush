using UnityEngine;

/// <summary>SP_0032 (드래곤) ? 아군 회복 시 적 전체에 고정피해.</summary>
[CreateAssetMenu(fileName = "SP_0032", menuName = "Data/Special/DamageOnAllyHeal")]
public class DamageOnAllyHealData : SpecialSkillData
{
    [Header("수치")] 
    [SerializeField] private float damage = 50f;

    public float Damage => damage;
}
