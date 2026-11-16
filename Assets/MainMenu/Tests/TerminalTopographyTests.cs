using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SilentSuns.Menu.Tests
{
    public partial class TerminalMenuTests
    {
        private static GameObject GroundBlock(string name, Vector3 position, Vector3 size)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = size;
            return block;
        }

        [UnityTest, Category("Menu"), Category("Surface"), Category("Topography")]
        public IEnumerator TopographyReadsRealGroundAndStaysAlignedWhileTurning()
        {
            yield return LoadSurface();
            var player = SurfacePlayer();
            ((Behaviour)player).enabled = false;
            Vector3 origin = new Vector3(10000, 52, 10000);
            player.GetComponent<Rigidbody>().isKinematic = true;
            player.transform.SetPositionAndRotation(origin, Quaternion.identity);
            var objects = new GameObject("Topography test ground");
            var ground = GroundBlock("Flat ground", origin + Vector3.down * 3, new Vector3(100, 1, 100));
            ground.transform.SetParent(objects.transform);
            var ledge = GroundBlock("Raised ledge", origin + new Vector3(16, 2, 15), new Vector3(13, 10, 20));
            ledge.transform.SetParent(objects.transform);
            var ramp = GroundBlock("Sloped ground", origin + new Vector3(-4, 2, 15), new Vector3(10, 1, 25));
            ramp.transform.rotation = Quaternion.Euler(-25, 0, 0);
            ramp.transform.SetParent(objects.transform);
            var trigger = GroundBlock("Trigger is not terrain", origin + new Vector3(0, 20, -20), Vector3.one * 8);
            trigger.GetComponent<Collider>().isTrigger = true;
            trigger.transform.SetParent(objects.transform);
            var prop = GroundBlock("Held prop is not terrain", origin + new Vector3(0, 10, -20), Vector3.one * 5);
            prop.AddComponent<Rigidbody>().isKinematic = true;
            prop.transform.SetParent(objects.transform);

            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(20, 10, 20) };
            var heights = new float[33, 33];
            for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z, x] = x / 32f;
            data.SetHeights(0, 0, heights);
            var terrain = Terrain.CreateTerrainGameObject(data);
            terrain.transform.position = origin + new Vector3(-30, -2.5f, -10);
            terrain.transform.SetParent(objects.transform);
            Physics.SyncTransforms();
            var map = SurfaceMap();
            var topo = ReadMember(map, "topography");
            var sample = topo.GetType().GetMethod("SampleHeight", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That((float)sample.Invoke(topo, new object[] { origin + new Vector3(0, 0, -20) }),
                Is.EqualTo(origin.y - 2.5f).Within(.02f), "Ignore triggers and Rigidbody props above the ground.");
            Assert.That((float)sample.Invoke(topo, new object[] { terrain.transform.position + new Vector3(8, 5, 10) }),
                Is.EqualTo(terrain.transform.position.y + 4).Within(.1f), "Use the actual TerrainCollider height.");
            Assert.That(float.IsNaN((float)sample.Invoke(topo, new object[] { origin + Vector3.right * 90 })), Is.True,
                "A place with no collision surface must stay unknown.");
            for (int i = 0; i < 35; i++) yield return null;
            Assert.That(ReadMember(topo, "hasMap"), Is.True);
            var texture = (Texture2D)ReadMember(topo, "texture");
            var colors = texture.GetPixels32();
            Assert.That(colors.Count(c => c.a >= 90 && c.g > c.r), Is.GreaterThan(50), "The slope must produce contour lines.");
            Assert.That(colors.Count(c => c.a > 170 && c.r > c.g), Is.GreaterThan(10), "The ledge must produce steep-ground edges.");
            var image = (RawImage)ReadMember(topo, "image");
            Assert.That(image.GetComponentInParent<Mask>().showMaskGraphic, Is.False);
            Assert.That(image.raycastTarget, Is.False);
            var system = (Component)Object.FindFirstObjectByType(GameplayType("ObjectiveSystem"));
            var objectives = SurveyObjectives(system);
            objectives[0].transform.position = origin + new Vector3(16, 7, 15);
            objectives[1].transform.position = origin + new Vector3(-20, 0, -5);
            yield return null;
            Object.Destroy(CaptureGameplay("topography-ground-and-objectives"));
            player.transform.rotation = Quaternion.Euler(0, 90, 0);
            yield return new WaitForSecondsRealtime(.9f);
            Assert.That(Mathf.DeltaAngle(image.rectTransform.localEulerAngles.z, 90), Is.EqualTo(0).Within(.01f));
            Vector3 beforeMove = player.transform.position;
            player.transform.position += Vector3.forward * 2;
            // Wait for the visual follow to settle before checking the exact distance conversion.
            float settleDeadline = Time.realtimeSinceStartup + 3;
            while (Time.realtimeSinceStartup < settleDeadline)
            {
                yield return null;
                if (Vector3.Distance((Vector3)ReadMember(map, "mapPosition"), player.transform.position) < .001f) break;
            }
            // Two metres at the display's 180-unit radius / 40-metre range = nine UI units.
            Vector3 patchCenter = (Vector3)ReadMember(topo, "drawnCenter");
            Vector2 pointInPatch = new Vector2(beforeMove.x - patchCenter.x, beforeMove.z - patchCenter.z) * 4.5f;
            Vector3 shownPoint = image.rectTransform.anchoredPosition3D + image.rectTransform.localRotation * pointInPatch;
            Assert.That(shownPoint.x, Is.EqualTo(9).Within(.05f),
                "At a 90-degree heading, the previous player location is nine UI units right, even if the patch has refreshed.");
            var pause = Object.FindFirstObjectByType<TerminalPause>();
            pause.Pause();
            yield return null;
            int scanPosition = (int)ReadMember(topo, "nextSample");
            for (int i = 0; i < 8; i++) yield return null;
            Assert.That(ReadMember(topo, "nextSample"), Is.EqualTo(scanPosition));
            Assert.That(map.GetComponent<Canvas>().enabled, Is.False);
            pause.Resume();
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(map.GetComponent<Canvas>().enabled, Is.True);
            Object.Destroy(objects);
            Object.Destroy(data);
            yield return LoadSurface();
            Assert.That(texture == null, Is.True, "The generated height texture must be released with the scene.");
        }

        [UnityTest, Category("Menu"), Category("Surface"), Category("Topography")]
        public IEnumerator TopographyMatchesExistingLevelGeometry()
        {
            yield return LoadSurface("PlanetSurfaceTest");
            var player = SurfacePlayer();
            ((Behaviour)player).enabled = false;
            var terrain = Object.FindFirstObjectByType<Terrain>();
            for (int i = 0; i < 35; i++) yield return null;
            var topo = ReadMember(SurfaceMap(), "topography");
            var heights = (float[])ReadMember(topo, "heights");
            Vector3 center = (Vector3)ReadMember(topo, "drawnCenter");
            float halfWidth = (float)ReadMember(topo, "halfWidth");
            float step = (float)ReadMember(topo, "step");
            int checkedPoints = 0;
            float biggestError = 0;
            var rows = new System.Text.StringBuilder("world_x,world_z,contour_height,terrain_height,error_metres\n");
            // Compare the cached map with the scene's existing TerrainData, without changing it.
            for (int z = 0; z < 81; z += 4)
            for (int x = 0; x < 81; x += 4)
            {
                Vector3 point = center + new Vector3(x * step - halfWidth, 0, z * step - halfWidth);
                float actual = heights[z * 81 + x];
                float expected = terrain.SampleHeight(point) + terrain.transform.position.y;
                Assert.That(float.IsNaN(actual), Is.False);
                float error = Mathf.Abs(actual - expected);
                biggestError = Mathf.Max(biggestError, error);
                Assert.That(error, Is.LessThan(.15f), "Map heights should agree with the level's TerrainData.");
                rows.AppendLine(System.FormattableString.Invariant($"{point.x:F3},{point.z:F3},{actual:F3},{expected:F3},{error:F4}"));
                checkedPoints++;
            }
            TestContext.WriteLine($"PlanetSurfaceTest: {checkedPoints} existing terrain samples; max height error: {biggestError:F4}m.");
            string folder = System.Environment.GetEnvironmentVariable("SILENT_SUNS_MENU_CAPTURES");
            if (!string.IsNullOrEmpty(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "existing-terrain-samples.csv"), rows.ToString());
            }

            // ObjectiveCheck has the team's blockout geometry. Its raised blocks should also appear.
            yield return LoadSurface();
            player = SurfacePlayer();
            ((Behaviour)player).enabled = false;
            for (int i = 0; i < 35; i++) yield return null;
            topo = ReadMember(SurfaceMap(), "topography");
            heights = (float[])ReadMember(topo, "heights");
            var known = heights.Where(h => !float.IsNaN(h)).ToArray();
            Assert.That(known.Max() - known.Min(), Is.GreaterThan(2), "Existing raised geometry must be visible in the height samples.");
            var texture = (Texture2D)ReadMember(topo, "texture");
            Assert.That(texture.GetPixels32().Count(c => c.a >= 95 && c.g > c.r), Is.Zero,
                "The flat ObjectiveCheck floor must not invent height contours from physics rounding noise.");
            Assert.That(texture.GetPixels32().Count(c => c.a > 170 && c.r > c.g), Is.GreaterThan(10),
                "The actual blockout edges should be drawn, not just a flat background.");
            Object.Destroy(CaptureGameplay("existing-gameplay-topography"));
            player.transform.Rotate(0, 90, 0);
            yield return new WaitForSecondsRealtime(.7f);
            Object.Destroy(CaptureGameplay("existing-gameplay-topography-turned"));
            player.transform.position += new Vector3(5, 0, 3);
            for (int i = 0; i < 35; i++) yield return null;
            Object.Destroy(CaptureGameplay("existing-gameplay-topography-moved"));
        }

        [UnityTest, Category("Menu"), Category("Surface"), Category("Topography")]
        public IEnumerator BlockOutlinesKeepTheirRealCornersAcrossMovementAndRescans()
        {
            yield return LoadSurface();
            var player = SurfacePlayer();
            ((Behaviour)player).enabled = false;
            var map = SurfaceMap();
            ((Behaviour)map).enabled = false;
            Vector3 origin = new Vector3(12000, 52, 12000);
            player.transform.SetPositionAndRotation(origin, Quaternion.identity);
            var ground = GroundBlock("Outline test ground", origin + Vector3.down * 3, new Vector3(300, 1, 300));
            var block = GroundBlock("Rotated square block", origin + new Vector3(8, 1, 9), new Vector3(8, 7, 8));
            block.transform.rotation = Quaternion.Euler(0, 27, 0);
            var small = GroundBlock("Block between samples", origin + new Vector3(-8.25f, 0, -8.25f), new Vector3(.8f, 5, .8f));
            var hidden = GroundBlock("Block underneath the ground", origin + new Vector3(20, -10, 20), Vector3.one * 3);
            Physics.SyncTransforms();
            var topo = ReadMember(map, "topography");
            var type = topo.GetType();
            var begin = type.GetMethod("BeginScan", BindingFlags.NonPublic | BindingFlags.Instance);
            var update = type.GetMethod("Update");
            var texture = (Texture2D)ReadMember(topo, "texture");
            float step = (float)ReadMember(topo, "step");
            float halfWidth = (float)ReadMember(topo, "halfWidth");
            Color32[] firstPixels = null;
            Vector3 firstCenter = default;

            // Small moves, grid-boundary crossings, and turning must leave each shape at the same world points.
            foreach (var move in new[] { Vector3.zero, new Vector3(.3f, 0, .2f), new Vector3(2.1f, 0, -1.8f), new Vector3(-5.2f, 0, 4.7f) })
            {
                Vector3 position = origin + move;
                player.transform.position = position;
                begin.Invoke(topo, new object[] { position });
                for (int i = 0; i < 26; i++) update.Invoke(topo, new object[] { position, 73f });
                Assert.That(ReadMember(topo, "nextSample"), Is.EqualTo(-1));
                var center = (Vector3)ReadMember(topo, "drawnCenter");
                Assert.That(center.x / step, Is.EqualTo(Mathf.Round(center.x / step)).Within(.001f));
                var pixels = texture.GetPixels32();
                float metresPerPixel = halfWidth * 2 / texture.width;
                int blockPixels = 0, smallPixels = 0;
                for (int z = 0; z < texture.height; z++)
                for (int x = 0; x < texture.width; x++)
                {
                    var color = pixels[z * texture.width + x];
                    if (color.a < 170 || color.r <= color.g) continue;
                    Vector3 point = center + new Vector3((x + .5f) * metresPerPixel - halfWidth, 0,
                        (z + .5f) * metresPerPixel - halfWidth);
                    float largeError = DistanceToBlockEdge(block.GetComponent<BoxCollider>(), point);
                    float smallError = DistanceToBlockEdge(small.GetComponent<BoxCollider>(), point);
                    Assert.That(Mathf.Min(largeError, smallError), Is.LessThan(metresPerPixel),
                        "Amber pixels must trace an actual visible box edge, not rounded height bands or hidden blocks.");
                    if (largeError < smallError) blockPixels++; else smallPixels++;
                }
                Assert.That(blockPixels, Is.GreaterThan(60));
                Assert.That(smallPixels, Is.GreaterThan(3), "A block smaller than the sample spacing must still appear.");
                if (firstPixels == null) { firstPixels = pixels; firstCenter = center; }
                else
                {
                    int dx = Mathf.RoundToInt((center.x - firstCenter.x) / metresPerPixel);
                    int dz = Mathf.RoundToInt((center.z - firstCenter.z) / metresPerPixel);
                    // Check the same world area in the overlapping textures, well away from their edges.
                    for (int z = 80; z < 240; z++)
                    for (int x = 80; x < 240; x++)
                        Assert.That(pixels[(z - dz) * texture.width + x - dx], Is.EqualTo(firstPixels[z * texture.width + x]),
                            "Rescanning after walking must not redraw a stationary object's shape differently.");
                }
            }
            Object.Destroy(ground); Object.Destroy(block); Object.Destroy(small); Object.Destroy(hidden);
            yield return null;
        }

        [UnityTest, Category("Menu"), Category("Surface"), Category("Topography")]
        public IEnumerator NavigationMotionStaysAlignedAndRefreshesWithoutBlanking()
        {
            yield return LoadSurface();
            var player = SurfacePlayer();
            ((Behaviour)player).enabled = false;
            player.GetComponent<Rigidbody>().isKinematic = true;
            player.transform.rotation = Quaternion.identity;
            yield return new WaitForSecondsRealtime(.9f);
            for (int i = 0; i < 35; i++) yield return null;
            var map = SurfaceMap();
            var topo = ReadMember(map, "topography");
            var image = (RawImage)ReadMember(topo, "image");
            Vector3 before = player.transform.position;
            player.transform.position += Vector3.forward * 2;
            player.transform.rotation = Quaternion.Euler(0, 90, 0);
            yield return null;
            float heading = (float)ReadMember(map, "mapHeading");
            Assert.That(heading, Is.GreaterThan(0).And.LessThan(89), "A turn should ease in rather than jump in one frame.");
            Vector3 displayedPosition = (Vector3)ReadMember(map, "mapPosition");
            Assert.That(displayedPosition.z - before.z, Is.GreaterThan(0).And.LessThan(1.99f));
            Assert.That(Mathf.DeltaAngle(image.rectTransform.localEulerAngles.z, heading), Is.EqualTo(0).Within(.01f),
                "Ground and compass must use the same heading while easing.");
            yield return new WaitForSecondsRealtime(.9f);
            Assert.That(Mathf.DeltaAngle((float)ReadMember(map, "mapHeading"), 90), Is.EqualTo(0).Within(.05f));
            Assert.That(Vector3.Distance((Vector3)ReadMember(map, "mapPosition"), player.transform.position), Is.LessThan(.02f));

            // A new scan should replace the old patch only when ready. Walking must not blank the map.
            ((Behaviour)map).enabled = false;
            Vector3 scanStart = (Vector3)ReadMember(topo, "drawnCenter");
            var update = topo.GetType().GetMethod("Update");
            for (int frame = 0; frame < 35; frame++)
            {
                update.Invoke(topo, new object[] { scanStart + Vector3.forward * 16, 90f });
                Assert.That(image.enabled, Is.True, "Keep the previous terrain patch visible during an ordinary refresh.");
                yield return null;
            }
            // A real teleport must still clear unrelated terrain while the destination is scanned.
            update.Invoke(topo, new object[] { scanStart + Vector3.forward * 200, 90f });
            Assert.That(image.enabled, Is.False);
        }

        private static float DistanceToBlockEdge(BoxCollider box, Vector3 point)
        {
            // Test against the actual transformed rectangle, including its yaw and non-uniform scale.
            Vector3 half = box.size * .5f;
            float closest = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 Corner(int n) => box.transform.TransformPoint(box.center + new Vector3(
                    n == 0 || n == 3 ? -half.x : half.x, 0, n < 2 ? -half.z : half.z));
                Vector3 a = Corner(i), b = Corner((i + 1) % 4);
                a.y = b.y = point.y;
                Vector3 edge = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(point - a, edge) / edge.sqrMagnitude);
                closest = Mathf.Min(closest, Vector3.Distance(point, a + edge * t));
            }
            return closest;
        }

        [UnityTest, Category("Menu"), Category("Surface"), Category("Topography")]
        public IEnumerator ObjectiveHeightShowsAboveBelowAndClearsOnTheSameLevel()
        {
            yield return LoadSurface();
            var player = SurfacePlayer();
            ((Behaviour)player).enabled = false;
            var system = (Component)Object.FindFirstObjectByType(GameplayType("ObjectiveSystem"));
            var objectives = SurveyObjectives(system);
            objectives[1].transform.position = player.transform.position + Vector3.forward * 300;
            var map = SurfaceMap();
            foreach (float height in new[] { 12f, -8f, 0f })
            {
                objectives[0].transform.position = player.transform.position + new Vector3(0, height, 8);
                yield return null;
                yield return null;
                var detail = (TMP_Text)ReadMember(map, "targetDetail");
                var marker = map.GetComponentsInChildren<Image>(true).First(i => i.name == "Objective height " + (string)ReadMember(objectives[0], "Title"));
                Assert.That(marker.gameObject.activeSelf, Is.EqualTo(height != 0));
                if (height != 0)
                {
                    StringAssert.Contains(height > 0 ? "12M ABOVE" : "8M BELOW", detail.text);
                    Assert.That(Mathf.DeltaAngle(marker.rectTransform.localEulerAngles.z, height > 0 ? 0 : 180), Is.EqualTo(0).Within(.1f));
                }
                else Assert.That(detail.text, Is.EqualTo("8 M"));
                Assert.That(ReadMember(objectives[0], "IsComplete"), Is.False);
                Object.Destroy(CaptureGameplay("objective-height-" + height));
            }
        }
    }
}
