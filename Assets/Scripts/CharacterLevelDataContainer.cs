using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CharacterLevelData
{
    public int maxHP;
    public int attack;
    public int defense;
    public float critRate;
    public float critDamage;
    public int speed;
    public CharacterLevelData(int maxHP, int attack, int defense, float critRate, float critDamage, int speed)
    {
        this.maxHP = maxHP;
        this.attack = attack;
        this.defense = defense;
        this.critRate = critRate;
        this.critDamage = critDamage;
        this.speed = speed;
    }
}
public struct EnemyLevelData
{
    public int maxHP;
    public int attack;
    public int defense;
    public int speed;
    public float K;
    public EnemyLevelData(int maxHP, int attack, int defense, int speed,float K)
    {
        this.maxHP = maxHP;
        this.attack = attack;
        this.defense = defense;
        this.speed = speed;
        this.K = K;
    }
}
public class LevelDataContainer
{
    public static Dictionary<string, Dictionary<int, CharacterLevelData>> CharacterLevelData;
    public static Dictionary<string, Dictionary<int, EnemyLevelData>> EnemyLevelData;
}
