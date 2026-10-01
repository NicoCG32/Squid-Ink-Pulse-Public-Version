using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquidInkPulse.Tests.PlayMode
{
    public sealed class MobileCameraFramingPlayModeTests : InputTestFixture
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/Game/ZonaEpipelagica.unity",
            "Assets/Scenes/Game/ZonaAbisopelagica.unity"
        };
        private static readonly float[] Aspects = { 16f / 9f, 19.5f / 9f, 20f / 9f };

        public override void Setup()
        {
            base.Setup();
            Time.timeScale = 1f;
            RuntimeGadgetInventory.ResetForRuntime();
            RuntimeInkPulseState.ResetForRuntime();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                Object.DestroyImmediate(root);
            RuntimeGadgetInventory.ResetForRuntime();
            RuntimeInkPulseState.ResetForRuntime();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Follow_KeepsPlayerAndGrazeVisibleAtVerticalLimitsAcrossMobileAspects()
        {
            foreach (string scenePath in ScenePaths)
            {
                yield return PrepareScene(scenePath);
                PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
                Camera camera = Camera.main;
                CameraController controller = camera.GetComponent<CameraController>();
                Collider2D body = player.GetComponent<Collider2D>();
                Collider2D graze = player.GetComponentInChildren<GrazeDetector>().GetComponent<Collider2D>();
                Assert.That(BoundaryReferenceResolver.TryResolveInnerVerticalRange(
                    BoundaryReferenceDomain.Player, body.bounds.extents.y, out Vector2 playerRange), Is.True);

                foreach (float aspect in Aspects)
                {
                    camera.aspect = aspect;
                    foreach (float y in new[] { playerRange.x, (playerRange.x + playerRange.y) * 0.5f, playerRange.y })
                    {
                        player.transform.position = new Vector3(player.transform.position.x, y, 0f);
                        Physics2D.SyncTransforms();
                        yield return new WaitForSeconds(2f);
                        Assert.That(controller.CurrentMode, Is.EqualTo(CameraEventMode.Follow));
                        string context = $"{scenePath}, aspect={aspect}, playerY={y}";
                        AssertVisible(camera, body.bounds, "player collider", context);
                        AssertVisible(camera, graze.bounds, "graze collider", context);
                        AssertComposition(camera, context);
                    }
                }

                camera.aspect = Aspects[0];
                yield return new WaitForSeconds(2f);
                float originalX = camera.transform.position.x;
                graze.enabled = false;
                yield return new WaitForSeconds(2f);
                Assert.That(camera.transform.position.x, Is.EqualTo(originalX).Within(0.01f),
                    $"{scenePath}: disabling the graze trigger must not change the framing envelope");
                graze.enabled = true;
            }
        }

        [UnityTest]
        public IEnumerator SpawnPositions_RespectAuthoredBoundariesAndCameraEdgeAcrossMobileAspects()
        {
            foreach (string scenePath in ScenePaths)
            {
                yield return PrepareScene(scenePath);
                PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
                Camera camera = Camera.main;
                LevelSpawner spawner = Object.FindFirstObjectByType<LevelSpawner>();
                Assert.That(spawner, Is.Not.Null);
                ZoneSpawnProfile profile = typeof(LevelSpawner)
                    .GetField("zoneSpawnProfile", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(spawner) as ZoneSpawnProfile;
                Assert.That(profile, Is.Not.Null, $"{scenePath}: authored spawn profile");
                Assert.That(BoundaryReferenceResolver.TryResolveInnerVerticalRange(
                    BoundaryReferenceDomain.Player, profile.VerticalPadding, out Vector2 spawnRange), Is.True);
                string[] tags = { EnemyTagCatalog.Generic, EnemyTagCatalog.Mine, EnemyTagCatalog.Pufferfish,
                    EnemyTagCatalog.FishingRod, EnemyTagCatalog.Ray, EnemyTagCatalog.Jellyfish };

                foreach (float aspect in Aspects)
                {
                    camera.aspect = aspect;
                    foreach (float y in new[] { spawnRange.x, spawnRange.y })
                    {
                        player.transform.position = new Vector3(player.transform.position.x, y, 0f);
                        Physics2D.SyncTransforms();
                        yield return new WaitForSeconds(2f);
                        float rightEdge = camera.ViewportToWorldPoint(
                            new Vector3(1f, 0.5f, -camera.transform.position.z)).x;
                        string context = $"{scenePath}, aspect={aspect}, playerY={y}";
                        Random.State randomState = Random.state;
                        try
                        {
                            Random.InitState(517);
                            for (int sample = 0; sample < 16; sample++)
                            {
                                Assert.That(SpawnPositionResolver.TryCalculateCoinSpawnPosition(camera,
                                    profile.VerticalPadding, profile.SpawnDistanceFromCameraRight, out Vector3 coin), Is.True, context);
                                AssertSpawnPoint(coin, rightEdge, spawnRange, context);
                                float viewportY = camera.WorldToViewportPoint(coin).y;
                                Assert.That(viewportY, Is.InRange(0f, 1f), $"{context}: coin vertical visibility");
                                Assert.That(SpawnPositionResolver.TryCalculatePortalSpawnPosition(camera,
                                    profile.VerticalPadding, profile.SpawnDistanceFromCameraRight, out Vector3 portal), Is.True, context);
                                AssertSpawnPoint(portal, rightEdge, spawnRange, context);
                                foreach (string tag in tags)
                                {
                                    Assert.That(SpawnPositionResolver.TryCalculateEnemySpawnPosition(camera,
                                        player.transform, null, profile, tag, out Vector3 enemy), Is.True, context);
                                    AssertSpawnPoint(enemy, rightEdge, spawnRange, $"{context}: {tag}");
                                }
                            }
                        }
                        finally
                        {
                            Random.state = randomState;
                        }
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator WideEvent_ShowsFullVerticalCompositionAndReturnsToFollow()
        {
            foreach (string scenePath in ScenePaths)
            {
                yield return PrepareScene(scenePath);
                PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
                Camera camera = Camera.main;
                CameraController controller = camera.GetComponent<CameraController>();
                float normalSize = camera.orthographicSize;
                Assert.That(BoundaryReferenceResolver.TryResolveInnerVerticalRange(
                    BoundaryReferenceDomain.Camera, 0f, out Vector2 cameraRange), Is.True);

                foreach (float aspect in Aspects)
                {
                    camera.aspect = aspect;
                    Assert.That(controller.BeginFullVerticalViewHold(0.05f, 0f), Is.True);
                    yield return new WaitForSeconds(2f);
                    Assert.That(controller.CurrentMode, Is.EqualTo(CameraEventMode.WideEvent));
                    string context = $"{scenePath}, aspect={aspect}";
                    float bottom = camera.ViewportToWorldPoint(new Vector3(0.5f, 0f, -camera.transform.position.z)).y;
                    float top = camera.ViewportToWorldPoint(new Vector3(0.5f, 1f, -camera.transform.position.z)).y;
                    Assert.That(bottom, Is.EqualTo(cameraRange.x).Within(0.01f), context);
                    Assert.That(top, Is.EqualTo(cameraRange.y).Within(0.01f), context);
                    AssertVisible(camera, player.GetComponent<Collider2D>().bounds, "player collider", context);

                    controller.EndFullVerticalViewHold();
                    yield return new WaitForSeconds(2f);
                    Assert.That(controller.CurrentMode, Is.EqualTo(CameraEventMode.Follow), context);
                    Assert.That(camera.orthographicSize, Is.EqualTo(normalSize).Within(0.01f), context);
                }
            }
        }

        private static IEnumerator PrepareScene(string path)
        {
            Time.timeScale = 1f;
            RuntimeGadgetInventory.ResetForRuntime();
            RuntimeInkPulseState.ResetForRuntime();
            yield return SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
            yield return null;
            PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
            Assert.That(player, Is.Not.Null);
            player.SetMovementSuppressed(true);
            // Isolate camera geometry from progression/spawns while the real
            // CameraController keeps running through its normal PlayerLoop.
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                string name = behaviour.GetType().Name;
                if (name.EndsWith("Spawner") || name == "BossEventDirector" || name == "RunProgressionDirector")
                    behaviour.enabled = false;
            }
            Time.timeScale = 10f;
        }

        private static void AssertVisible(Camera camera, Bounds bounds, string element, string context)
        {
            Vector3 minimum = camera.WorldToViewportPoint(bounds.min);
            Vector3 maximum = camera.WorldToViewportPoint(bounds.max);
            const float tolerance = 0.0001f;
            Assert.That(minimum.x, Is.GreaterThanOrEqualTo(-tolerance), $"{context}: {element} left={minimum.x}");
            Assert.That(minimum.y, Is.GreaterThanOrEqualTo(-tolerance), $"{context}: {element} bottom={minimum.y}");
            Assert.That(maximum.x, Is.LessThanOrEqualTo(1f + tolerance), $"{context}: {element} right={maximum.x}");
            Assert.That(maximum.y, Is.LessThanOrEqualTo(1f + tolerance), $"{context}: {element} top={maximum.y}");
        }

        private static void AssertComposition(Camera camera, string context)
        {
            Assert.That(BoundaryReferenceResolver.TryResolveInnerVerticalRange(
                BoundaryReferenceDomain.Camera, 0f, out Vector2 range), Is.True);
            float bottom = camera.ViewportToWorldPoint(new Vector3(0.5f, 0f, -camera.transform.position.z)).y;
            float top = camera.ViewportToWorldPoint(new Vector3(0.5f, 1f, -camera.transform.position.z)).y;
            Assert.That(bottom, Is.GreaterThanOrEqualTo(range.x - 0.01f), context);
            Assert.That(top, Is.LessThanOrEqualTo(range.y + 0.01f), context);
        }

        private static void AssertSpawnPoint(Vector3 point, float rightEdge, Vector2 playerRange, string context)
        {
            Assert.That(point.x, Is.GreaterThanOrEqualTo(rightEdge), context);
            Assert.That(point.y, Is.InRange(playerRange.x, playerRange.y), context);
        }
    }
}
