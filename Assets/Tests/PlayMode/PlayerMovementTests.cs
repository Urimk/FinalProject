using NUnit.Framework;
using UnityEngine;

public class PlayerMovementTests
{
    private PlayerMovement playerMovement;

    [SetUp]
    public void SetUp()
    {
        var player = new GameObject();
        playerMovement = player.AddComponent<PlayerMovement>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerMovement.gameObject);
    }

    [Test]
    public void TestDefaultSpeed()
    {
        // Verify default speed value (set in Inspector or script).
        Assert.AreEqual(0, playerMovement.Speed); // Default should match the serialized field value.
    }

    [Test]
    public void TestSetSpeed()
    {
        // Test the setter for Speed.
        playerMovement.Speed = 10f;
        Assert.AreEqual(10f, playerMovement.Speed);
    }

    [Test]
    public void TestDefaultJumpPower()
    {
        // Verify default jump power value.
        Assert.AreEqual(0, playerMovement.JumpPower); // Default should match the serialized field value.
    }

    [Test]
    public void TestSetJumpPower()
    {
        // Test the setter for JumpPower.
        playerMovement.JumpPower = 15f;
        Assert.AreEqual(15f, playerMovement.JumpPower);
    }

    [Test]
    public void TestGroundLayerSetter()
    {
        // Test the setter for GroundLayer.
        LayerMask testLayer = LayerMask.GetMask("Default");
        playerMovement.GroundLayer = testLayer;

        Assert.AreEqual(testLayer, playerMovement.GroundLayer);
    }

    [Test]
    public void TestIsGroundedMethod()
    {
        // Add a BoxCollider2D to the player GameObject
        var boxCollider = playerMovement.gameObject.AddComponent<BoxCollider2D>();
        boxCollider.size = new Vector2(1, 1); // Ensure appropriate collider size.

        // Explicitly call the Awake method to initialize the script components
        var initializeMethod = playerMovement.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        initializeMethod.Invoke(playerMovement, null);

        // Create a ground GameObject
        var ground = new GameObject();
        ground.AddComponent<BoxCollider2D>();
        ground.transform.position = playerMovement.transform.position - new Vector3(0, 1, 0); // Place below the player.

        // Set the groundLayer mask in the PlayerMovement script
        playerMovement.GroundLayer = LayerMask.GetMask("Ground");

        // Set the ground GameObject to the "Ground" layer
        ground.layer = LayerMask.NameToLayer("Ground");

        // Assert that the isGrounded method returns true
        var isGroundedMethod = playerMovement.GetType().GetMethod("isGrounded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(isGroundedMethod, "isGrounded method not found");
        bool isGrounded = (bool)isGroundedMethod.Invoke(playerMovement, null);

        Assert.IsTrue(isGrounded);
    }

}
