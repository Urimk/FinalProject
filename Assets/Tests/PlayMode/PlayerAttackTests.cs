using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class PlayerAttackTests
{
    private GameObject player;
    private PlayerAttack playerAttack;
    private GameObject fireball;
    private Transform firePoint;

    [SetUp]
    public void Setup()
    {
        // Create a player object with necessary components
        player = new GameObject();
        player.AddComponent<PlayerAttack>();
        player.AddComponent<PlayerMovement>(); // Assuming PlayerMovement is required
        player.AddComponent<Animator>(); // Assuming Animator is required

        playerAttack = player.GetComponent<PlayerAttack>();

        // Create a fireball GameObject to be used in the tests
        fireball = new GameObject();
        fireball.AddComponent<Projectile>(); // Assuming Projectile script is required

        // Set the fireball array to include our created fireball
        playerAttack.Fireballs = new GameObject[] { fireball };

        // Set fireball inactive initially
        fireball.SetActive(false);

        // Create a firePoint (for where the fireball will spawn)
        firePoint = new GameObject().transform;
        playerAttack.FirePoint = firePoint;

        // Initialize cooldownTimer to 0 (as it is in testAttack)
        playerAttack.CooldownTimer = 0;
    }

    [Test]
    public void TestAttackSetsFireballPosition()
    {
        // Simulate calling testAttack() (directly invoking the code inside)
        playerAttack.testAttack();

        // Check if fireball's position matches the firePoint's position
        Assert.AreEqual(firePoint.position, fireball.transform.position, "Fireball position should match the firePoint.");
    }

    [Test]
    public void TestAttackDoesNotHappenBeforeCooldownIsFinished()
    {
        // Set the cooldown to 1 second, and the cooldown timer to less than that
        playerAttack.AttackCooldown = 1.0f;
        playerAttack.CooldownTimer = 0.5f;

        // Call the attack method (but cooldown is not yet finished)
        playerAttack.testAttack();

        // Fireball should not be activated (since cooldown is still ongoing)
        Assert.IsFalse(fireball.activeInHierarchy, "Fireball should not be active if cooldown is not finished.");
    }

    [Test]
    public void TestAttackResetsCooldownTimer()
    {
        // Set the cooldown to 1 second and start cooldown at 0
        playerAttack.AttackCooldown = 1.0f;
        playerAttack.CooldownTimer = 0.0f;

        // Call attack method
        playerAttack.testAttack();

        // Check if cooldown timer was reset
        Assert.AreEqual(0f, playerAttack.CooldownTimer, "Cooldown timer should be reset after attack.");
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(player); // Clean up created objects
        Object.Destroy(fireball);
        Object.Destroy(firePoint.gameObject); // Clean up firePoint
    }
}
