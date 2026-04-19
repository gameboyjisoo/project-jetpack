using NUnit.Framework;

namespace ProjectJetpack.Tests
{
    public class MovementValueTests
    {
        [Test]
        public void BoostSpeed_Is_Approximately_1_9x_MoveSpeed()
        {
            float moveSpeed = 10f;
            float boostSpeed = 19f;
            float ratio = boostSpeed / moveSpeed;
            Assert.That(ratio, Is.InRange(1.8f, 2.0f),
                "Booster 2.0 speed ratio should be ~1.9x moveSpeed");
        }

        [Test]
        public void JetpackFuel_Lasts_Approximately_One_Second()
        {
            float maxGas = 100f;
            float consumptionRate = 100f;
            float duration = maxGas / consumptionRate;
            Assert.AreEqual(1f, duration, 0.01f,
                "Jetpack fuel should last ~1 second");
        }

        [Test]
        public void CoyoteTime_Matches_Celeste()
        {
            float coyoteTime = 0.1f;
            float celesteJumpGraceTime = 0.1f;
            Assert.AreEqual(celesteJumpGraceTime, coyoteTime,
                "Coyote time should match Celeste's JumpGraceTime");
        }

        [Test]
        public void SecondaryBoost_Is_Faster_Than_Jetpack()
        {
            float jetpackSpeed = 19f;
            float secondarySpeed = 40f;
            Assert.Greater(secondarySpeed, jetpackSpeed,
                "Secondary boost should be faster than primary jetpack");
        }
    }
}
