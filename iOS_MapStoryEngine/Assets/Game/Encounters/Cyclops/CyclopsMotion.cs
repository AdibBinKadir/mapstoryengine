using UnityEngine;

namespace Odyssey.Cyclops
{
    // Layers combat poses over the imported Idle clip while keeping the AR root in place.
    internal sealed class CyclopsMotion
    {
        private readonly Transform boss;
        private readonly Transform spine;
        private readonly Transform head;
        private readonly Arm left;
        private readonly Arm right;
        private readonly Transform[] bones;
        private readonly Quaternion[] idleRotations;
        private readonly Quaternion standing;
        private bool hasOffsets;

        internal CyclopsMotion(Transform boss)
        {
            this.boss = boss;
            standing = boss.rotation;
            Transform[] rig = boss.GetComponentsInChildren<Transform>(true);
            spine = FindBone(rig, "Spine_");
            head = FindBone(rig, "Head_");
            left = new Arm(boss, rig, "L");
            right = new Arm(boss, rig, "R");
            bones = new[] { spine, head, left.Upper, left.Forearm, right.Upper, right.Forearm };
            idleRotations = new Quaternion[bones.Length];
        }

        // Unity updates the Animator after Update. Remove last frame's offsets first so they
        // cannot accumulate on bones that are unkeyed in Idle or while tracking is paused.
        internal void RestoreIdle()
        {
            if (!hasOffsets)
                return;

            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null)
                    bones[i].localRotation = idleRotations[i];

            hasOffsets = false;
        }

        internal void Render(CyclopsFight fight, float recoil, float fallProgress)
        {
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null)
                    idleRotations[i] = bones[i].localRotation;
            hasOffsets = true;

            float progress = fight.Progress;
            bool slam = fight.CurrentAttack == CyclopsFight.Attack.Slam;
            if (fight.Current == CyclopsFight.Phase.Warning)
                Telegraph(slam, progress);
            else if (fight.Current == CyclopsFight.Phase.Recovery || fight.Current == CyclopsFight.Phase.Lost)
                Strike(slam, 1, fight.Finished ? 0.55f : 1 - Ease((progress - 0.18f) / 0.82f));
            else if (fight.Current == CyclopsFight.Phase.Won)
            {
                float buckle = Mathf.Sin(Mathf.Clamp01(fallProgress / 0.45f) * Mathf.PI);
                Twist(spine, 28 * buckle, 0, 0);
                Aim(left, new Vector3(left.Side, -0.5f, 0.6f), Vector3.down, 0.65f);
                Aim(right, new Vector3(right.Side, -0.5f, 0.6f), Vector3.down, 0.65f);
                float fall = Ease((fallProgress - 0.14f) / 0.86f);
                boss.rotation = standing * Quaternion.Euler(-86 * fall, 0, 8 * fall);
            }

            if (recoil > 0)
            {
                Twist(spine, -23 * recoil, 0, 10 * recoil);
                Twist(head, -14 * recoil, 0, -8 * recoil);
            }
        }

        private void Telegraph(bool slam, float progress)
        {
            if (progress >= 0.8f)
            {
                Strike(slam, Ease((progress - 0.8f) / 0.2f), 1);
                return;
            }

            float raise = Ease(progress / 0.68f);
            Twist(spine, -13 * raise, slam ? 0 : -30 * right.Side * raise, 4 * raise);
            Twist(head, 8 * raise, 0, 0);
            if (slam)
            {
                Aim(left, new Vector3(0.32f * left.Side, 1, 0.25f), new Vector3(0, 0.85f, 0.6f), raise);
                Aim(right, new Vector3(0.32f * right.Side, 1, 0.25f), new Vector3(0, 0.85f, 0.6f), raise);
            }
            else
            {
                Aim(right, new Vector3(right.Side, 0.28f, -0.35f), new Vector3(0.65f * right.Side, 0.8f, 0), raise);
            }
        }

        private void Strike(bool slam, float swing, float recover)
        {
            Twist(spine, Mathf.Lerp(-13, 23, swing) * recover,
                (slam ? 0 : Mathf.Lerp(-30, 32, swing) * right.Side) * recover, 0);

            if (slam)
            {
                SlamArm(left, swing, recover);
                SlamArm(right, swing, recover);
            }
            else
            {
                Aim(right,
                    Vector3.Lerp(new Vector3(right.Side, 0.28f, -0.35f), new Vector3(-0.7f * right.Side, -0.18f, 1), swing),
                    Vector3.Lerp(new Vector3(0.65f * right.Side, 0.8f, 0), new Vector3(-right.Side, -0.5f, 0.6f), swing),
                    recover);
            }
        }

        private void SlamArm(Arm arm, float swing, float recover)
        {
            Aim(arm,
                Vector3.Lerp(new Vector3(0.32f * arm.Side, 1, 0.25f), new Vector3(0.32f * arm.Side, -0.7f, 1), swing),
                Vector3.Lerp(new Vector3(0, 0.85f, 0.6f), new Vector3(0.1f * arm.Side, -1, 0.5f), swing),
                recover);
        }

        private void Aim(Arm arm, Vector3 upper, Vector3 forearm, float weight)
        {
            AimBone(arm.Upper, arm.Forearm, upper, weight);
            AimBone(arm.Forearm, arm.Hand, forearm, weight);
        }

        private void AimBone(Transform bone, Transform joint, Vector3 direction, float weight)
        {
            if (bone == null || joint == null)
                return;

            Quaternion adjustment = Quaternion.FromToRotation(joint.position - bone.position,
                boss.TransformDirection(direction));
            bone.rotation = Quaternion.Slerp(bone.rotation, adjustment * bone.rotation, weight);
        }

        private void Twist(Transform bone, float pitch, float yaw, float roll)
        {
            if (bone != null)
                bone.rotation = boss.rotation * Quaternion.Euler(pitch, yaw, roll)
                    * Quaternion.Inverse(boss.rotation) * bone.rotation;
        }

        private static float Ease(float amount)
        {
            return Mathf.SmoothStep(0, 1, Mathf.Clamp01(amount));
        }

        private static Transform FindBone(Transform[] rig, string prefix)
        {
            foreach (Transform bone in rig)
                if (bone.name.StartsWith(prefix, System.StringComparison.Ordinal))
                    return bone;

            return null;
        }

        private struct Arm
        {
            internal readonly Transform Upper;
            internal readonly Transform Forearm;
            internal readonly Transform Hand;
            internal readonly float Side;

            internal Arm(Transform boss, Transform[] rig, string side)
            {
                Upper = FindBone(rig, "Upperarm." + side + "_");
                Forearm = FindBone(rig, "Forearm." + side + "_");
                Hand = FindBone(rig, "Hand." + side + "_");
                Side = Upper == null ? 1 : Mathf.Sign(boss.InverseTransformPoint(Upper.position).x);
            }
        }
    }
}
