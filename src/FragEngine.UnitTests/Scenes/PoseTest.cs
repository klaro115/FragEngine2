using FragEngine.Constants;
using FragEngine.Extensions;
using FragEngine.Scenes;
using System.Numerics;

namespace FragEngine.UnitTests.Scenes;

/// <summary>
/// Unit tests for the <see cref="Pose"/> structure.
/// </summary>
[TestOf(typeof(Pose))]
public sealed class PoseTest
{
	#region Tests

	[Test]
	[Description("Tests that the identity pose can be correctly converted to an identity matrix.")]
	public void WorldMatrix_Identity_Test()
	{
		// Arrange:
		Pose pose = Pose.Identity;

		// Act:
		Matrix4x4 mtxWorld = pose.WorldMatrix;

		// Assert:
		Assert.That(mtxWorld.IsIdentity, Is.True, "Identity pose does not translate to identity world matrix!");
	}

	[TestCase(1, 1, 1)]
	[TestCase(2, 2, 2)]
	[TestCase(-2, 1, 3)]
	[Description("Tests that the identity pose can be correctly converted to an identity matrix.")]
	public void UnscaledMatrix_Test(float _scaleX, float _scaleY, float _scaleZ)
	{
		// Arrange:
		Vector3 originalPosition = new(-0.5f, 1, 2);
		Quaternion originalRotation = Quaternion.CreateFromYawPitchRoll(
			45 * MathConstants.Deg2Rad,
			22.5f * MathConstants.Deg2Rad,
			0);
		Vector3 originalScale = new(_scaleX, _scaleY, _scaleZ);

		Pose originalPose = new(originalPosition, originalRotation, originalScale);

		// Act:
		Matrix4x4 mtxUnscaled = originalPose.UnscaledMatrix;
		Matrix4x4.Decompose(
			mtxUnscaled,
			out Vector3 finalScale,
			out Quaternion finalRotation,
			out Vector3 finalPosition);

		// Assert:
		bool isPositionEqual = finalPosition.ApproximatelyEqual(originalPosition);
		bool isRotationEqual = finalRotation.ApproximatelyEqual(originalRotation);
		bool isScaleUnscaled = finalScale.ApproximatelyEqual(Vector3.One);
		Assert.Multiple(() =>
		{
			Assert.That(isPositionEqual, Is.True, "Position does not match original.");
			Assert.That(isRotationEqual, Is.True, "Rotation does not match original.");
			Assert.That(isScaleUnscaled, Is.True, "Scale factors are not 1.");
		});
	}

	//...

	#endregion
}
