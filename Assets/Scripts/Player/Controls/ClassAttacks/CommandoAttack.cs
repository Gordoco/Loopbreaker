using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class CommandoAttack : AttackManager
{
    [SerializeField] private GameObject SecondaryAttackProjPrefab;
    [SerializeField] private GameObject PrimaryAttackParticleEffect;
    [SerializeField] private GameObject DiveBombExplosionPrefab;
    [SerializeField] private GameObject JetpackParticleEffect;
    [SerializeField] private GameObject RegenParticleEffect;
    [SerializeField] private Image StimImage;
    [SerializeField] private GameObject HitParticleEffect;
    [SerializeField] private GameObject GrenadePrefab;

    /// <summary>
    /// Implements the primary attack for the Commando class. Based around the existence of twin pistols
    /// that fire in tandem. Triggers two hits per shot as an intended feature, interacting with On-Hit
    /// effects.
    /// </summary>
    protected override void OnPrimaryAttack()
    {
        GameObject Weapon = null;
        for (int i = 0; i < gameObject.transform.childCount; i++) if (gameObject.transform.GetChild(i).tag == "Weapon") { Weapon = gameObject.transform.GetChild(i).gameObject; break; }
        GameObject Camera = null;
        for (int i = 0; i < gameObject.transform.childCount; i++) if (gameObject.transform.GetChild(i).tag == "MainCamera") { Camera = gameObject.transform.GetChild(i).gameObject; break; }

        StartCoroutine(WeaponSwell(Weapon.transform.GetChild(0).gameObject, statManager.GetStat(NumericalStats.AttackSpeed)));
        StartCoroutine(WeaponSwell(Weapon.transform.GetChild(1).gameObject, statManager.GetStat(NumericalStats.AttackSpeed)));

        Vector3 start1 = Camera.transform.position;
        Vector3 start2 = start1;

        Vector3 dir = Camera.transform.forward;
        dir.Normalize();

        RaycastHit hit1;
        RaycastHit hit2;

        Vector3 Gun1MuzzleLoc = Weapon.transform.GetChild(0).position + (dir * 0.5f);
        Vector3 Gun2MuzzleLoc = Weapon.transform.GetChild(1).position + (dir * 0.5f);

        GameObject MF1 = Instantiate(PrimaryAttackParticleEffect, Gun1MuzzleLoc, Quaternion.LookRotation(dir));
        GameObject MF2 = Instantiate(PrimaryAttackParticleEffect, Gun2MuzzleLoc, Quaternion.LookRotation(dir));

        if (Physics.Raycast(start1, dir, out hit1, (float)statManager.GetStat(NumericalStats.Range))) { /*Debug.Log("GUN 1 HIT OBJECT");*/ }
        if (Physics.Raycast(start2, dir, out hit2, (float)statManager.GetStat(NumericalStats.Range))) { /*Debug.Log("GUN 2 HIT OBJECT");*/ }

        if (hit1.collider != null && hit1.collider.gameObject != null && hit1.collider.gameObject.GetComponentInParent<HitManager>() != null && !hit1.collider.gameObject.GetComponentInParent<PlayerStatManager>()) 
        { 
            hit1.collider.gameObject.GetComponentInParent<HitManager>().Hit((float)statManager.GetStat(NumericalStats.PrimaryDamage));
            GameObject HP1 = Instantiate(HitParticleEffect, hit1.transform.position, Quaternion.LookRotation(dir));
        }
        if (hit2.collider != null && hit2.collider.gameObject != null && hit2.collider.gameObject.GetComponentInParent<HitManager>() != null && !hit2.collider.gameObject.GetComponentInParent<PlayerStatManager>()) 
        {
            hit2.collider.gameObject.GetComponentInParent<HitManager>().Hit((float)statManager.GetStat(NumericalStats.PrimaryDamage));
            GameObject HP2 = Instantiate(HitParticleEffect, hit2.transform.position, Quaternion.LookRotation(dir));
        }
    }

    /// <summary>
    /// Template method for showcasing in a visual way a Weapon-Fired event
    /// </summary>
    /// <param name="Weapon">The GameObject which owns all weapon models (can be the sole model)</param>
    /// <param name="AttackSpeed">Passed in parameter corresponding to the AttackSpeed stat of the entity</param>
    /// <returns></returns>
    private IEnumerator WeaponSwell(GameObject Weapon, double AttackSpeed)
    {
        //Debug.Log("Started Swell");
        float swellSpeed = 0.02f;
        for (int i = 0; i < 12; i++)
        {
            Weapon.transform.localScale += new Vector3(swellSpeed, swellSpeed, swellSpeed);
            yield return new WaitForSeconds(0.01f / (float)AttackSpeed);
        }
        for (int i = 0; i < 12; i++)
        {
            Weapon.transform.localScale += new Vector3(-swellSpeed, -swellSpeed, -swellSpeed);
            yield return new WaitForSeconds(0.01f / (float)AttackSpeed);
        }
        yield return null;
    }

    protected override void OnSecondaryAttack()
    {
        if (GrenadePrefab != null)
        {
            GameObject Camera = null;
            for (int i = 0; i < gameObject.transform.childCount; i++) if (gameObject.transform.GetChild(i).tag == "MainCamera") { Camera = gameObject.transform.GetChild(i).gameObject; break; }

            GameObject proj = Instantiate(GrenadePrefab);
            if (proj.GetComponent<ProjectileCreator>() == null)
            {
                Destroy(proj);
                return;
            }
            float forwardOffset = 8;
            proj.GetComponent<ProjectileCreator>().InitializeProjectile(gameObject, Camera.transform.position + (Camera.transform.forward * forwardOffset), Camera.transform.forward, 0);
            bool server;
            if (isServer)
            {
                server = true;
                if (statManager.GetStat(NumericalStats.SecondaryCooldown) < 0.25) SecondaryFullAuto = true;
            }
            else server = false;
            proj.GetComponent<GrenadeTimedExplosion>().Init(gameObject, 20, (float)statManager.GetStat(NumericalStats.SecondaryDamage), server);
        }
    }

    protected override void OnAbility1()
    {
        if (StimImage) StimImage.enabled = true;

        StatManager manager = GetComponent<StatManager>();
        double[] vals = null;
        if (isServer)
        {
            if (manager.GetHealth() - 25 <= 0) manager.DealDamage(manager.GetHealth() - 1);
            else manager.DealDamage(25); //Deal Damage

            vals = new double[3];
            vals[0] = manager.GetStat(NumericalStats.AttackSpeed);
            vals[1] = manager.GetStat(NumericalStats.MovementSpeed);
            vals[2] = manager.GetStat(NumericalStats.JumpHeight);

            manager.ModifyStat(NumericalStats.AttackSpeed, vals[0]);
            manager.ModifyStat(NumericalStats.MovementSpeed, vals[1]);
            manager.ModifyStat(NumericalStats.JumpHeight, vals[2]);
        }
        if (vals == null) vals = new double[3];
        StartCoroutine(ResetStats(manager, 5f, vals));
    }

    private IEnumerator ResetStats(StatManager manager, float time, double[] vals)
    {
        yield return new WaitForSeconds(time);
        if (isServer)
        {
            manager.ModifyStat(NumericalStats.AttackSpeed, -vals[0]);
            manager.ModifyStat(NumericalStats.MovementSpeed, -vals[1]);
            manager.ModifyStat(NumericalStats.JumpHeight, -vals[2]);
        }

        if (StimImage) StimImage.enabled = false;
    }

    protected override void OnAbility2()
    {
        if (RegenParticleEffect) RegenParticleEffect.SetActive(true);
        double val = 0;
        StatManager manager = GetComponent<StatManager>();

        if (isServer)
        {
            val = manager.GetStat(NumericalStats.HealthRegenSpeed) * (1 - (1/manager.GetStat(NumericalStats.Ability2Damage)));
            manager.ModifyStat(NumericalStats.HealthRegenSpeed, -val);
        }
        StartCoroutine(ResetHealing(manager, 3, val));
    }

    private IEnumerator ResetHealing(StatManager manager, float time, double val)
    {
        yield return new WaitForSeconds(time);
        if (isServer)
        {
            manager.ModifyStat(NumericalStats.HealthRegenSpeed, val);
        }
        if (RegenParticleEffect) RegenParticleEffect.SetActive(false);
    }

    protected override void OnAbility3()
    {
        CharacterController controller = GetComponent<CharacterController>();
        StatManager manager = GetComponent<StatManager>();
        if (JetpackParticleEffect != null) JetpackParticleEffect.SetActive(true);
        GetComponent<PlayerMove>().SetFlying(true);
        //StartCoroutine(JetpackBoost(controller, manager));
        StartCoroutine(EndJetpack(manager.GetStat(NumericalStats.JumpHeight)/8));
    }

    IEnumerator EndJetpack(double time)
    {
        yield return new WaitForSeconds((float)time);
        if (JetpackParticleEffect != null) JetpackParticleEffect.SetActive(false);
        GetComponent<PlayerMove>().SetFlying(false);
    }

    Vector3 start;
    int count = 0;
    IEnumerator JetpackBoost(CharacterController controller, StatManager manager)
    {
        start = transform.position;
        if (JetpackParticleEffect != null) JetpackParticleEffect.SetActive(true);
        GetComponent<PlayerMove>().SetFlying(true);
        if (isServer) ClientsToggleFlying(true);
        while (count < 50)
        {
            if (isClient && controller.enabled) controller.Move(Vector3.up * (float)manager.GetStat(NumericalStats.JumpHeight) * 0.02f);
            count++;
            yield return new WaitForSeconds(0.02f);
        }
        count = 0;
        if (JetpackParticleEffect != null) JetpackParticleEffect.SetActive(false);
        GetComponent<PlayerMove>().SetFlying(false);
        if (isServer) ClientsToggleFlying(false);
    }

    [ClientRpc]
    void ClientsToggleFlying(bool b)
    {
        GetComponent<PlayerMove>().SetFlying(b);
    }

    protected override void OnAbility4()
    {
        float startLoc = transform.position.y;
        PlayerStatManager manager = GetComponent<PlayerStatManager>();
        if (isServer) manager.ToggleCCImmune(true);
        GetComponent<PlayerMove>().SetTempGravity(200);
        if (isClient) StartCoroutine(CheckForGrounded(startLoc));
    }

    private IEnumerator CheckForGrounded(float startLoc)
    {
        while (!GetComponent<CharacterController>().enabled || !GetComponent<CharacterController>().isGrounded)
        {
            yield return new WaitForSeconds(0.01f);
        }
        TriggerExplosion(startLoc);
    }

    [Command]
    private void TriggerExplosion(float startLoc)
    {
        float endLoc = transform.position.y;
        float magnitude = Mathf.Clamp(startLoc - endLoc, 0, 100f);
        PlayerStatManager manager = GetComponent<PlayerStatManager>();
        GameObject explosionObj = Instantiate(DiveBombExplosionPrefab);
        ExplosionCreator explosion = explosionObj.GetComponent<ExplosionCreator>();
        explosionObj.transform.localScale *= (float)manager.GetStat(NumericalStats.Range) / 10;
        explosion.InitializeExplosion(gameObject, transform.position, (float)manager.GetStat(NumericalStats.Range) / 2, (float)manager.GetStat(NumericalStats.Ability4Damage) * magnitude, true);
        manager.ToggleCCImmune(false);
    }
}
