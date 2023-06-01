// Uncomment to enable the debug view
// #define DEBUG_ENABLE

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Template;

internal static class VecUtil {
    public static Vector3 FromFloat3(float f) {
        return new Vector3(f, f, f);
    }

    public static Vector2 FromFloat2(float f) {
        return new Vector2(f, f);
    }

    public static Vector3 Min(this Vector3 left, float right) {
        return new Vector3(
            Math.Min(left.X, right),
            Math.Min(left.Y, right),
            Math.Min(left.Z, right)
        );
    }

    public static Vector3 Max(this Vector3 left, float right) {
        return new Vector3(
            Math.Max(left.X, right),
            Math.Max(left.Y, right),
            Math.Max(left.Z, right)
        );
    }

    public static bool IsZero(this Vector3 vector) {
        return vector.X == 0 && vector.Y == 0 && vector.Z == 0;
    }
}

internal readonly struct Material {
    public readonly Vector3 diffuseColor;
    public readonly Vector3 ambientColor;
    public readonly Vector3 specularColor;
    public readonly float specularity;
    public readonly Vector3 mirrorColor;

    public bool IsMirror => !mirrorColor.IsZero();
    public bool IsDiffuse => !diffuseColor.IsZero();
    public bool HasSpecularity => !specularColor.IsZero() && specularity > 0.0f;

    private Material(Vector3 diffuseColor, Vector3 ambientColor, Vector3 specularColor, float specularity,
        Vector3 mirrorColor) {
        this.diffuseColor = diffuseColor;
        this.ambientColor = ambientColor;
        this.specularColor = specularColor;
        this.specularity = specularity;
        this.mirrorColor = mirrorColor;
    }

    public static Material Diffuse(Vector3 diffuseColor) {
        return new Material(diffuseColor, diffuseColor, Vector3.Zero, 0f, Vector3.Zero);
    }

    public static Material Plastic(Vector3 diffuseColor, float specularity = 1.0f) {
        return new Material(diffuseColor, diffuseColor, new Vector3(0.4f, 0.4f, 0.4f), specularity, Vector3.Zero);
    }

    public static Material Metal(Vector3 diffuseColor, float specularity = 1.0f) {
        return new Material(diffuseColor, diffuseColor, diffuseColor, specularity, Vector3.Zero);
    }

    public static Material Mirror(Vector3 mirrorColor) {
        return new Material(Vector3.Zero, Vector3.Zero, Vector3.Zero, 0.0f, mirrorColor);
    }

    public static Material DiffuseMirror(Vector3 diffuseColor, Vector3 mirrorColor) {
        return new Material(diffuseColor, diffuseColor, Vector3.Zero, 0.0f, mirrorColor);
    }
}

internal struct IntersectResult {
    public readonly bool collision;
    public readonly float distance;

    private IntersectResult(bool collision, float distance) {
        this.collision = collision;
        this.distance = distance;
    }

    public static IntersectResult Collide(float distance) {
        return new IntersectResult(true, distance);
    }

    public static IntersectResult Miss() {
        return new IntersectResult(false, 0f);
    }
}

internal struct TraceResult {
    public readonly float distance;
    public readonly Vector3 color;
    public readonly IntersectResult intersectResult;

    public TraceResult(float distance, Vector3 color, IntersectResult intersectResult) {
        this.distance = distance;
        this.color = color;
        this.intersectResult = intersectResult;
    }
}

internal struct Light {
    public readonly Vector3 position;
    public readonly float intensity;

    public Light(Vector3 position, float intensity) {
        this.position = position;
        this.intensity = intensity;
    }
}

internal struct Plane {
    public readonly Vector3 center;
    public readonly Vector3 normal;
    public readonly Material material;

    public Plane(Vector3 center, Vector3 normal, Material material) {
        this.center = center;
        this.normal = normal;
        this.material = material;
    }
}

internal struct Sphere {
    public readonly Vector3 center;
    public readonly float radius;
    public readonly Material material;
    public readonly float diameter;
    public readonly float radiusSquared;

    public Sphere(Vector3 center, float radius, Material material) {
        this.center = center;
        this.radius = radius;
        this.material = material;
        diameter = radius * 2;
        radiusSquared = radius * radius;
    }
}

internal enum RayKind {
    Primary,
    Secondary,
    Shadow
}

internal struct Ray {
    public Vector3 origin;
    public Vector3 direction;
    public RayKind rayKind;

    private Ray(Vector3 origin, Vector3 direction, RayKind rayKind) {
        this.origin = origin;
        this.direction = direction;
        this.rayKind = rayKind;
    }

    public static Ray Primary(Vector3 origin, Vector3 direction) {
        return new Ray(origin, direction, RayKind.Primary);
    }

    public static Ray Secondary(Vector3 origin, Vector3 direction) {
        return new Ray(origin, direction, RayKind.Secondary);
    }

    public static Ray Shadow(Vector3 origin, Vector3 direction) {
        return new Ray(origin, direction, RayKind.Shadow);
    }
}

#if DEBUG_ENABLE
internal struct TracedRay {
    public readonly Ray ray;
    public readonly IntersectResult intersectResult;

    public TracedRay(Ray ray, IntersectResult intersectResult) {
        this.ray = ray;
        this.intersectResult = intersectResult;
    }
}
#endif

internal class MyApplication {
    private readonly Sphere[] _spheres = {
        new(new Vector3(2.5f, 0, 5), 1.0f, Material.Diffuse(new Vector3(1f, 0, 0))),
        new(new Vector3(3, 0, 8), 1.0f, Material.Plastic(new Vector3(0, 1, 0))),
        new(new Vector3(-3, 0, 8), 1.0f, Material.Diffuse(new Vector3(0, 0, 1)))
    };

    private readonly Light[] _lights = {
        new(new Vector3(-3, 1, -3), 1f)
    };

    private readonly Plane[] _planes = {
        new(new Vector3(0, 1f, 0), new Vector3(0, 1, 0), Material.Mirror(new Vector3(1f, 1f, 1f)))
    };

    // Ambient light color applied to objects shaded with the phong model
    private readonly Vector3 _ambientLightColor = VecUtil.FromFloat3(43f / 255f);

    private const float DebugSizeScaler = 0.3f;

    // The near clip plane distance
    private const float NearClip = 0.3f;

    // Degrees of FOV.
    // Default: 60 degrees
    private const float FieldOfView = 60f;

    // Position of the camera in world space
    private Vector3 _cameraPosition = new(0.0f, 0.0f, 0.0f);

    // The yaw and pitch of the camera.
    // These can be set manually or automatically using the LookAt(Vector3) function.
    private float _yaw;
    private float _pitch;

    public readonly Surface screen;

    // The camera's forward (looking direction) vector
    // Source: https://gamedev.stackexchange.com/a/190058
    private Vector3 CameraForwardDirection =>
        new((float)(Math.Cos(_pitch) * Math.Sin(_yaw)),
            (float)-Math.Sin(_pitch), (float)(Math.Cos(_pitch) * Math.Cos(_yaw)));

    // The camera's righ direction vector
    private Vector3 CameraRightDirection =>
        new((float)Math.Cos(_yaw), 0, (float)-Math.Sin(_yaw));

    // The camera's up direction vector
    private Vector3 CameraUpDirection =>
        Vector3.Cross(CameraRightDirection, CameraForwardDirection);

    public void OnKeyPress(KeyboardKeyEventArgs e) {
        _cameraPosition = e.Key switch {
            Keys.W => _cameraPosition + CameraForwardDirection,
            Keys.A => _cameraPosition - CameraRightDirection,
            Keys.S => _cameraPosition - CameraForwardDirection,
            Keys.D => _cameraPosition + CameraRightDirection,
            Keys.Space => _cameraPosition - CameraUpDirection,
            Keys.LeftShift or Keys.RightShift => _cameraPosition + CameraUpDirection,
            _ => _cameraPosition
        };
    }

#if DEBUG_ENABLE
    private readonly List<TracedRay> _tracedRays = new();
#endif

    public MyApplication(Surface screen) {
        this.screen = screen;
    }

    /// <summary>
    /// Set the yaw and pitch such that the camera looks at the provided destination vector.
    /// </summary>
    /// <param name="dest">The destination vector to look at</param>
    private void LookAt(Vector3 dest) {
        Vector3 destNormal = Vector3.Normalize(dest);
        _yaw = (float)Math.Atan2(destNormal.X, destNormal.Z);
        _pitch = (float)Math.Asin(-destNormal.Y);
    }

    /// <summary>
    /// Returns the intensity of the light at the provided hit point from the provided light.
    /// Does not take distance attenuation into account.
    /// </summary>
    /// <param name="hitPoint">The point to calculate the intensity for</param>
    /// <param name="light">The light to check from</param>
    /// <returns>The intensity of the light if there's an unobstructed path</returns>
    private float IntersectShadowLight(Vector3 hitPoint, Light light) {
        Ray ray = Ray.Shadow(hitPoint, light.position);

        bool intersectsObstruction = false;
        foreach (Sphere sphere in _spheres)
            if (IntersectsSphere(ray, sphere, 0.001f).collision)
                intersectsObstruction = true;

        return intersectsObstruction ? 0.0f : light.intensity;
    }

    /// <summary>
    /// Intersect a ray with a plane
    /// </summary>
    /// <param name="ray">The ray to intersect</param>
    /// <param name="plane">The plane to intersect with</param>
    /// <returns>The result of the trace</returns>
    private static IntersectResult IntersectPlane(Ray ray, Plane plane) {
        float t = (
                      -ray.origin.X * plane.normal.X
                      - ray.origin.Y * plane.normal.Y
                      - ray.origin.Z * plane.normal.Z + Vector3.Dot(plane.center, plane.normal))
                  /
                  Vector3.Dot(ray.direction, plane.normal);

        IntersectResult result = t > 0 ? IntersectResult.Collide(t) : IntersectResult.Miss();

#if DEBUG_ENABLE
        _tracedRays.Add(new TracedRay(ray, result));
#endif

        return result;
    }

    /// <summary>
    /// Intersect a ray with a sphere
    /// </summary>
    /// <param name="ray">The ray to intersect</param>
    /// <param name="sphere">The sphere to intersect with</param>
    /// <param name="epsilon">Optional epsilon value for shadow rays</param>
    /// <returns>The result of the trace</returns>
    private static IntersectResult IntersectsSphere(Ray ray, Sphere sphere, float epsilon = 0.0f) {
        Vector3 offsetOrigin = ray.origin - sphere.center;
        IntersectResult intersectResult = IntersectResult.Miss();

        float a = Vector3.Dot(ray.direction, ray.direction);
        float b = 2 * Vector3.Dot(offsetOrigin, ray.direction);
        float c = Vector3.Dot(offsetOrigin, offsetOrigin) - sphere.radiusSquared;

        float d = b * b - 4 * a * c;
        if (d >= 0) {
            float dSqrt = (float)Math.Sqrt(d);
            float a2 = 2 * a;

            float distance2 = (-b + dSqrt) / a2;
            float distance1 = (-b - dSqrt) / a2;

            float d1Eps = distance1 - epsilon;
            float d2Eps = distance2 - epsilon;

            float distance = Math.Min(Math.Max(distance1, 0), Math.Max(distance2, 0));
            float distanceEps = Math.Min(Math.Max(d1Eps, 0), Math.Max(d2Eps, 0));

            intersectResult = distanceEps > 0 ? IntersectResult.Collide(distance) : IntersectResult.Miss();
        }

#if DEBUG_ENABLE
        _tracedRays.Add(new TracedRay(ray, intersectResult));
#endif

        return intersectResult;
    }

    private static Vector3 PlanePhongShading(Vector3 hitPoint, Ray primaryRay, Plane plane, Light light) {
        return ShapePhongShading(hitPoint, primaryRay, plane.normal, plane.material, light);
    }

    private static Vector3 ShapePhongShading(Vector3 hitPoint, Ray primaryRay, Vector3 surfaceNormal, Material material,
        Light light) {
        // Shadow ray direction
        Vector3 lightDirectionNormal = Vector3.Normalize(light.position - hitPoint);
        Vector3 viewNormal = Vector3.Normalize(primaryRay.direction);

        Vector3 diffuseColor = Vector3.Zero;
        if (material.IsDiffuse) {
            float angle = Vector3.Dot(
                surfaceNormal,
                lightDirectionNormal
            );

            diffuseColor = material.diffuseColor // Kd
                           * Math.Max(0, angle); // max(0, N * L)
        }

        Vector3 specularColor = Vector3.Zero;
        if (material.HasSpecularity) {
            Vector3 specularDirection = lightDirectionNormal -
                                        2 * Vector3.Dot(lightDirectionNormal, surfaceNormal) * surfaceNormal;
            float specularity = Vector3.Dot(
                viewNormal,
                Vector3.Normalize(specularDirection)
            );

            specularColor = material.specularColor // Kd
                            * VecUtil.FromFloat3((float)Math.Pow(Math.Max(0, specularity), material.specularity));
        }

        return diffuseColor + specularColor;
    }

    /// <summary>
    /// Compute the light for a point in space resulting from the phong shading model
    /// </summary>
    /// <param name="hitPoint">The point to compute for</param>
    /// <param name="primaryRay">The primary (view) ray that hit the point</param>
    /// <param name="sphere">The sphere the point lies on</param>
    /// <param name="light">The light source to calculate for</param>
    /// <returns>The RGB color</returns>
    private static Vector3 SpherePhongShading(Vector3 hitPoint, Ray primaryRay, Sphere sphere, Light light) {
        Vector3 surfaceNormal = Vector3.Normalize(hitPoint - sphere.center);
        return ShapePhongShading(hitPoint, primaryRay, surfaceNormal, sphere.material, light);
    }

    /// <summary>
    /// Calculate a reflection ray.
    /// The resultant ray will reflect in the direction with the same
    /// angle to the normal as the viewray.
    /// </summary>
    /// <param name="viewRay">The ray to calculate the reflection of</param>
    /// <param name="normalRay">The surface normal ray</param>
    /// <returns></returns>
    private static Vector3 CalculateReflectionRay(Vector3 viewRay, Vector3 normalRay) {
        return viewRay - 2 * Vector3.Dot(viewRay, normalRay) * normalRay;
    }

    private TraceResult TracePlane(Ray ray, Plane plane, int secondaryBounceCount = 0) {
        IntersectResult intersectResult = IntersectPlane(ray, plane);
        if (!intersectResult.collision || intersectResult.distance - 0.01f <= 0)
            return new TraceResult(intersectResult.distance, Vector3.Zero, intersectResult);

        if (secondaryBounceCount > 32) return new TraceResult(intersectResult.distance, Vector3.Zero, intersectResult);

        Vector3 hitPoint = ray.origin + ray.direction * intersectResult.distance;
        Vector3 color = Vector3.Zero;

        if (plane.material.IsMirror) {
            secondaryBounceCount++;
            Vector3 secondaryRayDirection = CalculateReflectionRay(
                ray.direction,
                plane.normal
            );

            color += TraceSecondaryRay(hitPoint, secondaryRayDirection, secondaryBounceCount) *
                     plane.material.mirrorColor;
        }

        if (plane.material.IsDiffuse)
            foreach (Light light in _lights) {
                float lightIntensity = IntersectShadowLight(hitPoint, light);
                Vector3 intensityRgb = VecUtil.FromFloat3(lightIntensity);
                float distAttenuation = (float)(1 / Math.Pow(intersectResult.distance, 2));
                color +=
                    (intensityRgb * distAttenuation * PlanePhongShading(hitPoint, ray, plane, light))
                    .Max(0.0f); // Make sure the color stays positive
            }

        color += _ambientLightColor * plane.material.ambientColor;
        return new TraceResult(intersectResult.distance, color, intersectResult);
    }

    /// <summary>
    /// Trace a secondary ray to all other objects in the scene
    /// </summary>
    /// <param name="hitPoint">The origin of the secondary ray. Where the primary ray hit the object that is being reflected from</param>
    /// <param name="secondaryRayDirection">The direction of the secondary ray</param>
    /// <param name="secondaryBounceCount">The current bounce count</param>
    /// <returns>Returns the 'seen' color. Must be multiplied by the mirror color</returns>
    private Vector3 TraceSecondaryRay(Vector3 hitPoint, Vector3 secondaryRayDirection, int secondaryBounceCount) {
        float closestSphere = float.PositiveInfinity;
        Vector3 sphereColor = Vector3.Zero;
        foreach (Sphere secondarySphere in _spheres) {
            Ray secondaryRay = Ray.Secondary(hitPoint, secondaryRayDirection);
            TraceResult secondaryTraceResult = TraceSphere(
                secondaryRay,
                secondarySphere,
                secondaryBounceCount
            );

#if DEBUG_ENABLE
            _tracedRays.Add(new TracedRay(secondaryRay, secondaryTraceResult.intersectResult));
#endif

            if (secondaryTraceResult.distance - 0.01f > 0 && secondaryTraceResult.distance - 0.01f < closestSphere) {
                closestSphere = secondaryTraceResult.distance;
                sphereColor = secondaryTraceResult.color;
            }
        }

        float closestPlane = float.PositiveInfinity;
        Vector3 planeColor = Vector3.Zero;
        foreach (Plane secondaryPlane in _planes) {
            TraceResult secondaryTraceResult = TracePlane(
                Ray.Secondary(hitPoint, secondaryRayDirection),
                secondaryPlane,
                secondaryBounceCount
            );

            if (secondaryTraceResult.distance > 0 && secondaryTraceResult.distance < closestPlane) {
                closestPlane = secondaryTraceResult.distance;
                planeColor = secondaryTraceResult.color;
            }
        }

        return closestSphere < closestPlane ? sphereColor : planeColor;
    }

    private TraceResult TraceSphere(Ray ray, Sphere sphere, int secondaryBounceCount = 0) {
        IntersectResult intersectResult = IntersectsSphere(ray, sphere);

        // No intersections for this ray
        if (!intersectResult.collision || intersectResult.distance - 0.01f <= 0)
            return new TraceResult(intersectResult.distance, Vector3.Zero, intersectResult);

        // Too many secondary ray bounces
        if (secondaryBounceCount > 32) return new TraceResult(intersectResult.distance, Vector3.Zero, intersectResult);

        // Point on the surfac of the sphere
        Vector3 hitPoint = ray.origin + ray.direction * intersectResult.distance;
        Vector3 color = Vector3.Zero;

        // Compute reflections by casting secondary rays
        if (sphere.material.IsMirror) {
            secondaryBounceCount++;
            Vector3 secondaryRayDirection = CalculateReflectionRay(
                ray.direction,
                (hitPoint - sphere.center).Normalized()
            );

            color += TraceSecondaryRay(hitPoint, secondaryRayDirection, secondaryBounceCount) *
                     sphere.material.mirrorColor;
        }

        // Compute phong shading
        if (sphere.material.IsDiffuse)
            foreach (Light light in _lights) {
                float lightIntensity = IntersectShadowLight(hitPoint, light);
                Vector3 intensityRgb = VecUtil.FromFloat3(lightIntensity);
                float distanceAttenuation = 1 / intersectResult.distance * intersectResult.distance;

                color +=
                    intensityRgb * distanceAttenuation * SpherePhongShading(hitPoint, ray, sphere, light);
            }

        // Add ambient light
        color += _ambientLightColor * sphere.material.ambientColor;

        return new TraceResult(intersectResult.distance, color, intersectResult);
    }

    public void Tick() {
        screen.Clear(0);

        float planeHeight = NearClip * (float)Math.Tan(MathHelper.DegreesToRadians(FieldOfView * 0.5f)) * 2;
        float aspectRatio = (float)screen.width / screen.height;
        float planeWidth = planeHeight * aspectRatio;

        var viewParams = new Vector3(planeWidth, planeHeight, NearClip);

        int debugHeight = (int)Math.Floor(screen.height * DebugSizeScaler);
        int debugWidth = (int)Math.Floor(screen.width * DebugSizeScaler);

        int debugTopLeftX = screen.width - debugWidth;
        int debugTopLeftY = screen.height - debugHeight;

        bool IsInDebug(int x, int y) {
            return x >= debugTopLeftX && y >= debugTopLeftY;
        }

        for (int x = 0; x < screen.width; x++) Parallel.For(0, screen.height, y => TracePixel(x, y, viewParams));

#if DEBUG_ENABLE
        float scaleFactor = 1f;
        foreach (Sphere sphere in _spheres) {
            Vector2 screenSpaceCenter = WorldspaceToScreenspace(sphere.center.Xz, scaleFactor);
            Vector2i center = new(
                debugTopLeftX + (int) (screenSpaceCenter.X * DebugSizeScaler),
                debugTopLeftY + (int) (screenSpaceCenter.Y * DebugSizeScaler)
            );
            Console.WriteLine($"Trying to draw at {center}");
            
            float scaledRadius = sphere.radius / DebugSizeScaler;
            DrawCircle(center, scaledRadius);
        }
        
        foreach (TracedRay tracedRay in _tracedRays) {
            
        }

#endif
    }

    private void TracePixel(int x, int y, Vector3 viewParams) {
        Vector2 viewPointLocal2D =
            new Vector2(x, y) / new Vector2(screen.width, screen.height) - VecUtil.FromFloat2(0.5f);
        Vector3 viewPointLocal = new Vector3(viewPointLocal2D.X, viewPointLocal2D.Y, 1f) * viewParams;

        Vector3 viewPoint = _cameraPosition + CameraRightDirection * viewPointLocal.X +
                            CameraUpDirection * viewPointLocal.Y +
                            CameraForwardDirection * viewPointLocal.Z;

        Ray cameraPrimaryRay = Ray.Primary(_cameraPosition, Vector3.Normalize(viewPoint - _cameraPosition));

        Vector3 pixelColorSphere = Vector3.Zero;
        float nearestDistSphere = float.PositiveInfinity;
        foreach (Sphere sphere in _spheres) {
            TraceResult result = TraceSphere(cameraPrimaryRay, sphere);
            if (result.distance > 0 && nearestDistSphere > result.distance) {
                nearestDistSphere = result.distance;
                pixelColorSphere = result.color;
            }
        }

        Vector3 pixelColorPlane = Vector3.Zero;
        float nearestDistPlane = float.PositiveInfinity;
        foreach (Plane plane in _planes) {
            TraceResult result = TracePlane(cameraPrimaryRay, plane);
            if (result.distance > 0 && nearestDistPlane > result.distance) {
                nearestDistPlane = result.distance;
                pixelColorPlane = result.color;
            }
        }

        Vector3 finalPixelColor = nearestDistSphere < nearestDistPlane ? pixelColorSphere : pixelColorPlane;

#if DEBUG_ENABLE
        if (!IsInDebug(x, y)) {
            SetPixel(new Vector2i(x, y), finalPixelColor);
        }
#else
        SetPixel(new Vector2i(x, y), finalPixelColor);
#endif
    }

    private Vector2 WorldspaceToScreenspace(Vector2 input, float scaleFactor) {
        return new Vector2(
            (input.X + screen.width / 2f) / scaleFactor,
            (-input.Y + screen.height / 2f) / scaleFactor
        );
    }

    private void DrawCircle(Vector2i center, float radius) {
        for (int angle = 0; angle < 360; angle++) {
            float angleRad = MathHelper.DegreesToRadians(angle);
            int x = (int)(center.X + radius * Math.Cos(angleRad));
            int y = (int)(center.Y + radius * Math.Sin(angleRad));

            SetPixel(new Vector2i(x, y), VecUtil.FromFloat3(1));
        }
    }

    private void SetPixel(Vector2i position, Vector3 color) {
        screen.pixels[position.Y * screen.width + position.X] = ShiftColor(color);
    }

    private int ShiftColor(Vector3 color) {
        var clamped = new Vector3i(
            (int)Math.Floor(Math.Clamp(color.X, 0f, 1f) * 255f),
            (int)Math.Floor(Math.Clamp(color.Y, 0f, 1f) * 255f),
            (int)Math.Floor(Math.Clamp(color.Z, 0f, 1f) * 255f));
        return ((byte)clamped.X << 16) | ((byte)clamped.Y << 8) | (byte)clamped.Z;
    }

    public void OnMouseMove(MouseMoveEventArgs e) {
        _yaw += e.DeltaX / 360;
        _pitch += e.DeltaY / 360;
        
    }
}