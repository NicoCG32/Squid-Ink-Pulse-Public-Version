using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class SpawnProfileEnemyActivationTests
    {
        private const string EpipelagicProfilePath = "Assets/Implementation/Config/Spawning/ZonaEpipelagicaSpawnProfile.asset";
        private const string AbyssopelagicProfilePath = "Assets/Implementation/Config/Spawning/ZonaAbisopelagicaSpawnProfile.asset";

        [Test]
        public void EpipelagicProfile_EnablesRayWithValidPrefab()
        {
            GameObject prefab = AssertEnemyProfile(
                EpipelagicProfilePath,
                EnemyTagCatalog.Ray,
                expectedComponent: typeof(RayEnemy),
                minExpectedWeight: 0.01f);

            AssertEnemyPrefabTagAndLayerContract(prefab, EnemyTagCatalog.Ray);
        }

        [Test]
        public void AbyssopelagicProfile_EnablesJellyfishWithValidPrefab()
        {
            GameObject prefab = AssertEnemyProfile(
                AbyssopelagicProfilePath,
                EnemyTagCatalog.Jellyfish,
                expectedComponent: typeof(JellyfishEnemy),
                minExpectedWeight: 0.01f);

            AssertEnemyPrefabTagAndLayerContract(prefab, EnemyTagCatalog.Jellyfish);
        }

        [Test]
        public void AbyssopelagicProfile_JellyfishSeparatesBounceAndDeathColliders()
        {
            GameObject prefab = AssertEnemyProfile(
                AbyssopelagicProfilePath,
                EnemyTagCatalog.Jellyfish,
                expectedComponent: typeof(JellyfishEnemy),
                minExpectedWeight: 0.01f);

            Collider2D[] colliders = prefab.GetComponents<Collider2D>();
            Assert.That(colliders, Has.Length.EqualTo(2), "Jellyfish debe separar fisica de rebote y area letal.");

            CircleCollider2D bounceCollider = prefab.GetComponent<CircleCollider2D>();
            Assert.That(bounceCollider, Is.Not.Null, "Jellyfish debe tener CircleCollider2D superior para rebote.");
            Assert.That(bounceCollider.isTrigger, Is.False, "El CircleCollider2D de Jellyfish no debe ser trigger: representa contacto fisico/rebote.");

            BoxCollider2D deathCollider = prefab.GetComponent<BoxCollider2D>();
            Assert.That(deathCollider, Is.Not.Null, "Jellyfish debe tener BoxCollider2D inferior para muerte.");
            Assert.That(deathCollider.isTrigger, Is.True, "El BoxCollider2D de Jellyfish debe ser trigger: es el collider letal que procesa PlayerCollision.");
            Assert.That(deathCollider.offset.y, Is.LessThan(bounceCollider.offset.y), "El collider letal de Jellyfish debe quedar bajo el collider de rebote.");
        }

        [Test]
        public void RayMovement_StopsVerticalTravelAtPlayerBoundaryAndKeepsHorizontalTravel()
        {
            Vector3 start = new(2f, 4.9f, 0f);
            Vector2 verticalRange = new(-5f, 5f);

            Vector3 next = RayEnemy.CalculateNextPosition(
                start,
                2f,
                3f,
                1f,
                1f,
                true,
                verticalRange,
                out float nextVerticalDirection);

            Assert.That(next.x, Is.EqualTo(0f).Within(0.0001f), "Ray debe seguir avanzando horizontalmente.");
            Assert.That(next.y, Is.EqualTo(5f).Within(0.0001f), "Ray debe quedar limitado por el boundary superior.");
            Assert.That(nextVerticalDirection, Is.Zero, "Ray debe anular movimiento vertical al tocar un boundary.");
        }

        [Test]
        public void JellyfishTuning_DefaultBounceParametersAreActive()
        {
            JellyfishEnemyTuning tuning = new();

            Assert.That(tuning.BounceVerticalVelocity, Is.GreaterThan(0f));
            Assert.That(tuning.BounceDuration, Is.GreaterThan(0f));
            Assert.That(tuning.BounceCooldown, Is.GreaterThanOrEqualTo(0f));
        }

        private static void AssertEnemyPrefabTagAndLayerContract(GameObject prefab, string expectedRootTag)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            Assert.That(enemyLayer, Is.GreaterThanOrEqualTo(0), "Debe existir la layer Enemy.");

            Assert.That(prefab.CompareTag(expectedRootTag), Is.True, $"{prefab.name} debe declarar el tag logico solo en el root.");

            Transform[] transforms = prefab.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (Transform current in transforms)
            {
                Assert.That(current.gameObject.layer, Is.EqualTo(enemyLayer), $"{current.name} debe estar en layer Enemy para colision, graze y cleanup.");

                if (current.gameObject == prefab)
                {
                    continue;
                }

                Assert.That(current.CompareTag("Untagged"), Is.True, $"{current.name} no debe duplicar tags logicos de enemigo.");
            }
        }

        private static GameObject AssertEnemyProfile(
            string profilePath,
            string expectedTag,
            System.Type expectedComponent,
            float minExpectedWeight)
        {
            ZoneSpawnProfile profile = AssetDatabase.LoadAssetAtPath<ZoneSpawnProfile>(profilePath);
            Assert.That(profile, Is.Not.Null, $"No se encontro ZoneSpawnProfile: {profilePath}");

            SerializedObject serializedProfile = new(profile);
            SerializedProperty enemyProfiles = serializedProfile.FindProperty("enemyProfiles");
            Assert.That(enemyProfiles, Is.Not.Null, $"{profilePath} no expone enemyProfiles.");

            SerializedProperty matchingProfile = FindEnemyProfile(enemyProfiles, expectedTag);
            Assert.That(matchingProfile, Is.Not.Null, $"{profilePath} no contiene entrada para {expectedTag}.");

            float baseWeight = matchingProfile.FindPropertyRelative("baseWeight").floatValue;
            Assert.That(baseWeight, Is.GreaterThanOrEqualTo(minExpectedWeight), $"{expectedTag} debe estar habilitado con peso positivo.");

            Object prefabReference = matchingProfile.FindPropertyRelative("prefab").objectReferenceValue;
            Assert.That(prefabReference, Is.TypeOf<GameObject>(), $"{expectedTag} debe tener prefab asignado.");

            GameObject prefab = (GameObject)prefabReference;
            Assert.That(prefab.CompareTag(expectedTag), Is.True, $"{prefab.name} debe usar tag {expectedTag}.");
            Assert.That(prefab.GetComponent(expectedComponent), Is.Not.Null, $"{prefab.name} debe tener {expectedComponent.Name}.");
            return prefab;
        }

        private static SerializedProperty FindEnemyProfile(SerializedProperty enemyProfiles, string expectedTag)
        {
            for (int index = 0; index < enemyProfiles.arraySize; index++)
            {
                SerializedProperty profile = enemyProfiles.GetArrayElementAtIndex(index);
                string enemyTag = profile.FindPropertyRelative("enemyTag").stringValue;
                if (enemyTag == expectedTag)
                {
                    return profile;
                }
            }

            return null;
        }
    }
}
