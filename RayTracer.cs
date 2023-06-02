// Uncomment to enable the debug view
// #define DEBUG_ENABLE

using System.Collections.Concurrent;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Template;

/// <summary>
/// Utilities for working with vectors
/// </summary>
internal static class VecUtil {
    /// <summary>
    /// Create a vector with the same X, Y and Z component
    /// </summary>
    /// <param name="f">The component</param>
    /// <returns>The Vector3</returns>
    public static Vector3 FromFloat3(float f) {
        return new Vector3(f, f, f);
    }

    /// <summary>
    /// Create a vector with the same X and Y component
    /// </summary>
    /// <param name="f">THe component</param>
    /// <returns>The Vector2</returns>
    public static Vector2 FromFloat2(float f) {
        return new Vector2(f, f);
    }

    /// <summary>
    /// Compute the Vector3 with the individual components Max'd with the provided parameter
    /// </summary>
    /// <param name="left">The vector</param>
    /// <param name="right">The component to max with</param>
    /// <returns>The computed Vector3</returns>
    public static Vector3 Max(this Vector3 left, float right) {
        return new Vector3(
            Math.Max(left.X, right),
            Math.Max(left.Y, right),
            Math.Max(left.Z, right)
        );
    }

    /// <summary>
    /// Whether the Vector is a zero vector
    /// </summary>
    /// <param name="vector">The vector</param>
    /// <returns>True if all components are equal to zero</returns>
    public static bool IsZero(this Vector3 vector) {
        return vector.X == 0 && vector.Y == 0 && vector.Z == 0;
    }
}

/// <summary>
/// The material of a surface
/// </summary>
internal readonly struct Material {
    /// <summary>
    /// The diffuse color K_d
    /// </summary>
    public readonly Vector3 diffuseColor;
    /// <summary>
    /// The ambient color K_a
    /// </summary>
    public readonly Vector3 ambientColor;
    /// <summary>
    /// The specular color K_s
    /// </summary>
    public readonly Vector3 specularColor;
    /// <summary>
    /// The amount of specular highlight to create. The n power
    /// </summary>
    public readonly float specularity;
    /// <summary>
    /// The mirror color. K_m
    /// </summary>
    public readonly Vector3 mirrorColor;

    /// <summary>
    /// Whether the material has mirror properties
    /// </summary>
    public bool IsMirror => !mirrorColor.IsZero();
    /// <summary>
    /// WHether the material has diffuse properties
    /// </summary>
    public bool IsDiffuse => !diffuseColor.IsZero();
    /// <summary>
    /// Whether the material has a specular highlight
    /// </summary>
    public bool HasSpecularity => !specularColor.IsZero() && specularity > 0.0f;

    /// <summary>
    /// Create a new material
    /// </summary>
    /// <param name="diffuseColor">The diffuse color. K_d</param>
    /// <param name="ambientColor">The ambient light color. K_a</param>
    /// <param name="specularColor">The specular highlight color. K_s</param>
    /// <param name="specularity">The amount of specularity. n</param>
    /// <param name="mirrorColor">The mirror color. K_m</param>
    public Material(Vector3 diffuseColor, Vector3 ambientColor, Vector3 specularColor, float specularity,
        Vector3 mirrorColor) {
        this.diffuseColor = diffuseColor;
        this.ambientColor = ambientColor;
        this.specularColor = specularColor;
        this.specularity = specularity;
        this.mirrorColor = mirrorColor;
    }

    /// <summary>
    /// Create a normal diffuse material
    /// </summary>
    /// <param name="diffuseColor">The color of the material</param>
    /// <returns>The material</returns>
    public static Material Diffuse(Vector3 diffuseColor) {
        return new Material(diffuseColor, diffuseColor, Vector3.Zero, 0f, Vector3.Zero);
    }

    /// <summary>
    /// Create a plastic looking material
    /// </summary>
    /// <param name="diffuseColor">The color of the material</param>
    /// <param name="specularity">The amount of specular highlight to produce</param>
    /// <returns>The material</returns>
    public static Material Plastic(Vector3 diffuseColor, float specularity = 1.0f) {
        return new Material(diffuseColor, diffuseColor, new Vector3(0.4f, 0.4f, 0.4f), specularity, Vector3.Zero);
    }

    /// <summary>
    /// Create a metalic looking material
    /// </summary>
    /// <param name="diffuseColor">The color of the material</param>
    /// <param name="specularity">The amount of specular highlight to produce</param>
    /// <returns>The material</returns>
    public static Material Metal(Vector3 diffuseColor, float specularity = 1.0f) {
        return new Material(diffuseColor, diffuseColor, diffuseColor, specularity, Vector3.Zero);
    }

    /// <summary>
    /// Create a pure reflective object (i.e. a mirror)
    /// </summary>
    /// <param name="mirrorColor">The color of the mirror</param>
    /// <returns>The material</returns>
    public static Material Mirror(Vector3 mirrorColor) {
        return new Material(Vector3.Zero, Vector3.Zero, Vector3.Zero, 0.0f, mirrorColor);
    }

    /// <summary>
    /// Create a reflective object with a diffuse surface
    /// </summary>
    /// <param name="diffuseColor">The color of the object surface</param>
    /// <param name="mirrorColor">The color of the mirror</param>
    /// <returns>The material</returns>
    public static Material DiffuseMirror(Vector3 diffuseColor, Vector3 mirrorColor) {
        return new Material(diffuseColor, diffuseColor, Vector3.Zero, 0.0f, mirrorColor);
    }
}

/// <summary>
/// The result of a ray intersection
/// </summary>
internal struct IntersectResult {
    /// <summary>
    /// Whether the ray intersected something
    /// </summary>
    public readonly bool collision;
    /// <summary>
    /// The distance between the ray's origin and the collision point.
    /// 0f if collission is false.
    /// </summary>
    public readonly float distance;

    /// <summary>
    /// Create a new IntersectionResult
    /// </summary>
    /// <param name="collision">Whether the ray collided with anything</param>
    /// <param name="distance">The distance between the ray's origin and the collision point</param>
    private IntersectResult(bool collision, float distance) {
        this.collision = collision;
        this.distance = distance;
    }

    /// <summary>
    /// Create a result that collided
    /// </summary>
    /// <param name="distance">The distance between the ray's origin and the collision point</param>
    /// <returns>The IntersectionResult</returns>
    public static IntersectResult Collide(float distance) {
        return new IntersectResult(true, distance);
    }

    /// <summary>
    /// Create a result that did not collide
    /// </summary>
    /// <returns>The IntersectionResult</returns>
    public static IntersectResult Miss() {
        return new IntersectResult(false, 0f);
    }
}

/// <summary>
/// The result of a full trace
/// </summary>
internal struct TraceResult {
    /// <summary>
    /// The distance between the Ray's origin and the point computed
    /// </summary>
    public readonly float distance;
    /// <summary>
    /// The color at the point computed
    /// </summary>
    public readonly Vector3 color;
    /// <summary>
    /// The underlying ray intersection result
    /// </summary>
    public readonly IntersectResult intersectResult;

    /// <summary>
    /// Create a new trace result
    /// </summary>
    /// <param name="distance">The distance between the Ray's origin and the point computed</param>
    /// <param name="color">The color at the computed point</param>
    /// <param name="intersectResult">The underlying ray intersection result</param>
    public TraceResult(float distance, Vector3 color, IntersectResult intersectResult) {
        this.distance = distance;
        this.color = color;
        this.intersectResult = intersectResult;
    }
}

/// <summary>
/// A point light source
/// </summary>
internal struct Light {
    /// <summary>
    /// The position of the light source
    /// </summary>
    public readonly Vector3 position;
    /// <summary>
    /// The intensity of the light source
    /// </summary>
    public readonly float intensity;

    /// <summary>
    /// Create a new light source
    /// </summary>
    /// <param name="position">The position of the light source</param>
    /// <param name="intensity">The intensity of the light source</param>
    public Light(Vector3 position, float intensity) {
        this.position = position;
        this.intensity = intensity;
    }
}

/// <summary>
/// A plane
/// </summary>
internal struct Plane {
    /// <summary>
    /// The center point of the plane
    /// </summary>
    public readonly Vector3 center;
    /// <summary>
    /// The normal vector to the plane
    /// </summary>
    public readonly Vector3 normal;
    /// <summary>
    /// The material applied to the plane
    /// </summary>
    public readonly Material material;
    /// <summary>
    /// Whether the plane should have a tiled checkerboard texture
    /// </summary>
    public readonly bool isTiled;

    /// <summary>
    /// Create a new plane
    /// </summary>
    /// <param name="center">The center point of the plane</param>
    /// <param name="normal">The normal vector of the plane</param>
    /// <param name="material">The material to apply to the plane</param>
    /// <param name="isTiled">Whether a tiled texture should be applied. Refer to <see cref="Plane.Tiled"/></param>
    public Plane(Vector3 center, Vector3 normal, Material material, bool isTiled = false) {
        this.center = center;
        this.normal = normal;
        this.material = material;
        this.isTiled = true;
    }

    /// <summary>
    /// Create a plane with a 'tiled' texture, i.e. a checkerboard.
    /// Note that the texture is only visible if the Material applied to it has a diffuse color
    /// </summary>
    /// <param name="center">The center point of the plane</param>
    /// <param name="normal">The normal vector of the plane</param>
    /// <param name="material">The material to apply to the plane</param>
    /// <returns>The plane confgiured according to the provided parameters</returns>
    public static Plane Tiled(Vector3 center, Vector3 normal, Material material) {
        return new Plane(center, normal, material, true);
    }
}

/// <summary>
/// A spherical shape
/// </summary>
internal struct Sphere {
    /// <summary>
    /// The center coordinate
    /// </summary>
    public readonly Vector3 center;
    /// <summary>
    /// The radius of the sphere. r
    /// </summary>
    public readonly float radius;
    /// <summary>
    /// The material to apply
    /// </summary>
    public readonly Material material;
    /// <summary>
    /// The radius squared. r^2
    /// </summary>
    public readonly float radiusSquared;

    /// <summary>
    /// Create a new sphere
    /// </summary>
    /// <param name="center">The center coordinate of the sphere</param>
    /// <param name="radius">The radius of the sphere</param>
    /// <param name="material">The material to apply to the sphere</param>
    public Sphere(Vector3 center, float radius, Material material) {
        this.center = center;
        this.radius = radius;
        this.material = material;
        this.radiusSquared = radius * radius;
    }
}

/// <summary>
/// The kind of ray
/// </summary>
internal enum RayKind {
    /// <summary>
    /// Primary ray.
    /// Used for intersecting from the camera with the scene
    /// </summary>
    Primary,
    /// <summary>
    /// Secondary ray.
    /// Used for intersecting from a primary ray's intersection point to the scene
    /// for calculating reflections.
    /// </summary>
    Secondary,
    /// <summary>
    /// Shadow ray.
    /// Used for intersecting from a primary or secondary ray's intersection point
    /// to light sources in the scene.
    /// </summary>
    Shadow
}

/// <summary>
/// A ray
/// </summary>
internal struct Ray {
    /// <summary>
    /// The origin of the ray
    /// </summary>
    public Vector3 origin;
    /// <summary>
    /// The direction of the ray
    /// </summary>
    public Vector3 direction;
    /// <summary>
    /// The kind of ray
    /// </summary>
    public RayKind rayKind;

    /// <summary>
    /// Create a new ray
    /// </summary>
    /// <param name="origin">The ray's origin</param>
    /// <param name="direction">The ray's destination</param>
    /// <param name="rayKind">The kind of ray</param>
    private Ray(Vector3 origin, Vector3 direction, RayKind rayKind) {
        this.origin = origin;
        this.direction = direction;
        this.rayKind = rayKind;
    }

    /// <summary>
    /// Create a primary ray
    /// </summary>
    /// <param name="origin">The ray's origin</param>
    /// <param name="direction">The ray's destination</param>
    /// <returns>The ray</returns>
    public static Ray Primary(Vector3 origin, Vector3 direction) {
        return new Ray(origin, direction, RayKind.Primary);
    }

    /// <summary>
    /// Create a secondary ray
    /// </summary>
    /// <param name="origin">The ray's origin</param>
    /// <param name="direction">The ray's destination</param>
    /// <returns>The ray</returns>
    public static Ray Secondary(Vector3 origin, Vector3 direction) {
        return new Ray(origin, direction, RayKind.Secondary);
    }

    /// <summary>
    /// Create a shadow ray
    /// </summary>
    /// <param name="origin">The ray's origin</param>
    /// <param name="direction">The ray's destination</param>
    /// <returns>The ray</returns>
    public static Ray Shadow(Vector3 origin, Vector3 direction) {
        return new Ray(origin, direction, RayKind.Shadow);
    }
}

#if DEBUG_ENABLE
internal struct TracedRay {
    public readonly Ray ray;
    public readonly Vector3 hitPoint;
    public readonly IntersectResult intersectResult;

    public TracedRay(Ray ray, IntersectResult intersectResult, Vector3 hitPoint) {
        this.ray = ray;
        this.intersectResult = intersectResult;
        this.hitPoint = hitPoint;
    }
}
#endif

internal class RayTracer {
    /// <summary>
    /// Spheres in the scene
    /// </summary>
    private readonly Sphere[] _spheres = {
        new(new Vector3(2.5f, 0, 8), 1.0f, Material.Diffuse(new Vector3(1f, 0, 0))),
        new(new Vector3(3, 0, 5), 1.0f, Material.Plastic(new Vector3(0, 1, 0))),
        new(new Vector3(-3, 1, 8), 1.0f, Material.Mirror(new Vector3(1, 1, 1)))
    };

    /// <summary>
    /// Light sources in the scene
    /// </summary>
    private readonly Light[] _lights = {
        new(new Vector3(-3, 1, -3), 1f),
        new(new Vector3(33, 1, 10), 1f)
    };

    /// <summary>
    /// Planes in the scene
    /// </summary>
    private readonly Plane[] _planes = {
        Plane.Tiled(new Vector3(0, -1f, 0), new Vector3(0, 1, 0), new Material(
            new Vector3(1f, 1f, 1f), 
            VecUtil.FromFloat3(0.5f),
            Vector3.One,
            0.5f,
            new Vector3(1f, 1f, 1f)))
    };

    /// The scene's ambient light.
    /// Only applied to materials with an ambient light color component.
    private readonly Vector3 _ambientLightColor = VecUtil.FromFloat3(43f / 255f);

    /// <summary>
    /// The size of the debug view as a factor of the screen size.
    /// Only visible if DEBUG_ENABLE is defined at the top of the file
    /// </summary>
    private const float DebugSizeScaler = 0.3f;
    /// <summary>
    /// Near clip plane distance.
    /// Everything with a distance less than this to the camera
    /// will not be drawn.
    /// </summary>
    private const float NearClip = 0.3f;
    /// <summary>
    /// The field of view in degress.
    /// Default: 60 degrees (Unity's default)
    /// </summary>
    private const float FieldOfView = 60f;
    /// <summary>
    /// The maximum number of 'bounces' for a reflection (secondary) ray
    /// </summary>
    private const int ReflectionRecursionLimit = 32;
    /// <summary>
    /// The camera's starting position in world space
    /// </summary>
    private Vector3 _cameraPosition = new(0.0f, 0.0f, 0.0f);
    /// <summary>
    /// The camera's starting yaw
    /// </summary>
    private float _yaw;
    /// <summary>
    /// The camera's starting pitch
    /// </summary>
    private float _pitch;
    /// <summary>
    /// The parent screen surface
    /// </summary>
    public readonly Surface screen;
    /// <summary>
    /// The camera's forward (looking) direction
    /// </summary>
    // Source: https://gamedev.stackexchange.com/a/190058
    private Vector3 CameraForwardDirection =>
        new((float)(Math.Cos(_pitch) * Math.Sin(_yaw)),
            (float)-Math.Sin(_pitch), (float)(Math.Cos(_pitch) * Math.Cos(_yaw)));
    /// <summary>
    /// The camera's right direction
    /// </summary>
    private Vector3 CameraRightDirection =>
        new((float)Math.Cos(_yaw), 0, (float)-Math.Sin(_yaw));
    /// <summary>
    /// The camera's up direction
    /// </summary>
    private Vector3 CameraUpDirection =>
        Vector3.Cross(CameraRightDirection, CameraForwardDirection);
#if DEBUG_ENABLE
    /// <summary>
    /// List of traced rays for the debug view.
    /// </summary>
    private readonly ConcurrentBag<TracedRay> _tracedRays = new();
#endif
    
    /// <summary>
    /// Create a new ray tracer
    /// </summary>
    /// <param name="screen">The parent screen surface</param>
    public RayTracer(Surface screen) {
        this.screen = screen;
    }

    /// <summary>
    /// Key press handler
    /// </summary>
    /// <param name="e">The event arguments</param>
    public void OnKeyPress(KeyboardKeyEventArgs e) {
        Vector3 moveScaler = VecUtil.FromFloat3(0.05f);
        _cameraPosition = e.Key switch {
            Keys.W => _cameraPosition + CameraForwardDirection * moveScaler,
            Keys.A => _cameraPosition - CameraRightDirection * moveScaler,
            Keys.S => _cameraPosition - CameraForwardDirection * moveScaler,
            Keys.D => _cameraPosition + CameraRightDirection * moveScaler,
            Keys.Space => _cameraPosition - CameraUpDirection * moveScaler,
            Keys.LeftShift or Keys.RightShift => _cameraPosition + CameraUpDirection * moveScaler,
            _ => _cameraPosition
        };
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
    private IntersectResult IntersectPlane(Ray ray, Plane plane) {
        float t = (
                      -ray.origin.X * plane.normal.X
                      - ray.origin.Y * plane.normal.Y
                      - ray.origin.Z * plane.normal.Z + Vector3.Dot(plane.center, plane.normal))
                  /
                  Vector3.Dot(ray.direction, plane.normal);

        IntersectResult result = t > 0 ? IntersectResult.Collide(t) : IntersectResult.Miss();

#if DEBUG_ENABLE
        Task.Run(() => _tracedRays.Add(new TracedRay(ray, result, ray.origin + ray.direction * t)));
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
    private IntersectResult IntersectsSphere(Ray ray, Sphere sphere, float epsilon = 0.0f) {
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
        Task.Run(() => _tracedRays.Add(new TracedRay(ray, intersectResult, ray.origin + ray.direction * intersectResult.distance)));
#endif
        return intersectResult;
    }

    /// <summary>
    /// Compute phong shading for a plane
    /// </summary>
    /// <param name="hitPoint">The point on the plane where the ray hit the plane</param>
    /// <param name="primaryRay">The ray that hit the plane</param>
    /// <param name="plane">The plane itself</param>
    /// <param name="light">The light source to compute for</param>
    /// <returns>The color at the hit point</returns>
    private static Vector3 PlanePhongShading(Vector3 hitPoint, Ray primaryRay, Plane plane, Light light) {
        return ShapePhongShading(hitPoint, primaryRay, plane.normal, plane.material, light);
    }

    /// <summary>
    /// Compute phong shading for a shape
    /// </summary>
    /// <param name="hitPoint">The point on the shape where the ray hit the shape</param>
    /// <param name="primaryRay">The primary ray that hit the shape</param>
    /// <param name="surfaceNormal">The surface normal at the hit point</param>
    /// <param name="material">The material of the shape</param>
    /// <param name="light">The light source to compute for</param>
    /// <returns>Ther color at the hit point</returns>
    private static Vector3 ShapePhongShading(Vector3 hitPoint, Ray primaryRay, Vector3 surfaceNormal, Material material,
        Light light) {
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

    /// <summary>
    /// Trace a ray on a plane and compute the color for that ray
    /// </summary>
    /// <param name="ray">The ray</param>
    /// <param name="plane">The plane</param>
    /// <param name="secondaryBounceCount">Optional, how many times a secondary ray has bounced</param>
    /// <returns>The TraceResult</returns>
    private TraceResult TracePlane(Ray ray, Plane plane, int secondaryBounceCount = 0) {
        IntersectResult intersectResult = IntersectPlane(ray, plane);
        if (!intersectResult.collision || intersectResult.distance - 0.01f <= 0)
            return new TraceResult(intersectResult.distance, Vector3.Zero, intersectResult);

        if (secondaryBounceCount > ReflectionRecursionLimit) return new TraceResult(intersectResult.distance, Vector3.One, intersectResult);

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

                Vector3 tileColor = Vector3.One;
                if (plane.isTiled) {
                    // First calculate the UV coordinates
                    // Source: https://gamedev.stackexchange.com/a/172357
                    Vector3 e1 = Vector3.Normalize(Vector3.Cross(plane.normal, new Vector3(1f, 0f, 0f)));
                    if(e1 == VecUtil.FromFloat3(0f)) {
                        e1 = Vector3.Normalize(Vector3.Cross(plane.normal, new Vector3(0, 0, 1)));
                    }

                    Vector3 e2 = Vector3.Normalize(Vector3.Cross(plane.normal, e1));
                    float u = Vector3.Dot(e1, hitPoint);
                    float v = Vector3.Dot(e2, hitPoint);

                    int checkerboardColor = (int) u + (int) v & 1;
                    tileColor = VecUtil.FromFloat3(checkerboardColor);
                }
                
                color +=
                    (intensityRgb * distAttenuation * PlanePhongShading(hitPoint, ray, plane, light) * tileColor)
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
            Task.Run(() => _tracedRays.Add(new TracedRay(secondaryRay, secondaryTraceResult.intersectResult, hitPoint)));
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

    /// <summary>
    /// Trace a ray on a sphere and compute the color for that ray
    /// </summary>
    /// <param name="ray">The ray</param>
    /// <param name="sphere">The sphere</param>
    /// <param name="secondaryBounceCount">Optional, how many times a secondary ray has bounced</param>
    /// <returns>The TraceResult</returns>
    private TraceResult TraceSphere(Ray ray, Sphere sphere, int secondaryBounceCount = 0) {
        IntersectResult intersectResult = IntersectsSphere(ray, sphere);

        // No intersections for this ray
        if (!intersectResult.collision || intersectResult.distance - 0.01f <= 0)
            return new TraceResult(intersectResult.distance, Vector3.Zero, intersectResult);

        // Too many secondary ray bounces
        if (secondaryBounceCount > ReflectionRecursionLimit) return new TraceResult(intersectResult.distance, Vector3.Zero, intersectResult);

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

    private int DebugHeight => (int)Math.Floor(screen.height * DebugSizeScaler);
    private int DebugWidth => (int)Math.Floor(screen.width * DebugSizeScaler);
    
    private int DebugTopLeftX => screen.width - DebugWidth;
    private int DebugTopLeftY => screen.height - DebugHeight;

    private bool IsInDebugView(int x, int y) => x > DebugTopLeftX && y > DebugTopLeftY;

    public void Tick() {
#if DEBUG_ENABLE
        _tracedRays.Clear();
#endif
        screen.Clear(0);

        float planeHeight = NearClip * (float)Math.Tan(MathHelper.DegreesToRadians(FieldOfView * 0.5f)) * 2;
        float aspectRatio = (float)screen.width / screen.height;
        float planeWidth = planeHeight * aspectRatio;

        var viewParams = new Vector3(planeWidth, planeHeight, NearClip);

        for (int x = 0; x < screen.width; x++) {
            int x1 = x;
            Parallel.For(0, screen.height, y => TracePixel(x1, y, viewParams));
        }
        
#if DEBUG_ENABLE
        // camera
        DrawCircle(DebugOffsetCoordinates(_cameraPosition), 1f);
        
        // spheres
        foreach (Sphere sphere in _spheres) {
            Vector2i translated = DebugOffsetCoordinates(sphere.center);
            float scaledRadius = sphere.radius / DebugSizeScaler / 0.5f;
            DrawCircle(translated, scaledRadius);
        }

        TracedRay[] rays = _tracedRays.ToArray();
        Random random = new Random();
        const int debugNumRays = 500;
        for (int i = 0; i < debugNumRays; i++) {
            int randIdx = random.Next(0, rays.Length);
            TracedRay tracedRay = rays[randIdx];
            
            Vector2i translatedOrigin = ClampToDebugView(DebugOffsetCoordinates(tracedRay.ray.origin));
            Vector2i translatedHitPoint = ClampToDebugView(DebugOffsetCoordinates(tracedRay.hitPoint));

            Vector3 lineColor = tracedRay.ray.rayKind switch {
                RayKind.Primary => new Vector3(1f, 0f, 0f),
                RayKind.Secondary => new Vector3(0f, 1f, 0f),
                RayKind.Shadow => new Vector3(0f, 0f, 1f),
                _ => throw new ArgumentOutOfRangeException()
            };

            screen.Line(translatedOrigin.X, translatedOrigin.Y, translatedHitPoint.X, translatedHitPoint.Y, ShiftColor(lineColor));
        }

#endif
    }

    private Vector2i ClampToDebugView(Vector2i input) {
        return new Vector2i(
            Math.Clamp(input.X, DebugTopLeftX, screen.width),
            Math.Clamp(input.Y, DebugTopLeftY, screen.height)
        );
    }
    
    private Vector2i DebugOffsetCoordinates(Vector3 worldspaceCoordinates) {
        Vector2 screenSpaceCenter = WorldspaceToScreenspace(worldspaceCoordinates.Xz);
        float offsetX = worldspaceCoordinates.X / DebugSizeScaler / 0.1f;
        float offsetY = worldspaceCoordinates.Z / DebugSizeScaler / 0.1f;
        Vector2i coords = new(
            (int) ((screenSpaceCenter.X + offsetX) * DebugSizeScaler) + DebugTopLeftX, 
            (int) ((screenSpaceCenter.Y + offsetY - 30) * DebugSizeScaler) + DebugTopLeftY
        );

        return coords;
    }

    /// <summary>
    /// Trace a single pixel
    /// </summary>
    /// <param name="x">The X coordinate in screen space</param>
    /// <param name="y">The Y coordinate in screen space</param>
    /// <param name="viewParams">The view parameters</param>
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
        Vector2i pixelCoordinate = new Vector2i(x, y);
#if DEBUG_ENABLE
        if (!IsInDebugView(x, y)) {
            SetPixel(pixelCoordinate, finalPixelColor);
        }
#else
        SetPixel(pixelCoordinate, finalPixelColor);
#endif
    }

    /// <summary>
    /// Translate world space coordinates to screen space coordinates
    /// </summary>
    /// <param name="input">The input pixels</param>
    /// <param name="scaleFactor">Optional scale (zoom) factor</param>
    /// <returns>The translated coordinates</returns>
    private Vector2 WorldspaceToScreenspace(Vector2 input, float scaleFactor = 1.0f) {
        return new Vector2(
            (input.X + screen.width / 2f) / scaleFactor,
            (-input.Y + screen.height / 2f) / scaleFactor
        );
    }

    /// <summary>
    /// Draw a circle on the screen
    /// </summary>
    /// <param name="center">Screen space coordinates of the center</param>
    /// <param name="radius">The radius of the circle</param>
    private void DrawCircle(Vector2i center, float radius) {
        for (int angle = 0; angle < 360; angle++) {
            float angleRad = MathHelper.DegreesToRadians(angle);
            int x = (int)(center.X + radius * Math.Cos(angleRad));
            int y = (int)(center.Y + radius * Math.Sin(angleRad));

            SetPixel(new Vector2i(x, y), VecUtil.FromFloat3(1));
        }
    }

    /// <summary>
    /// Set a pixel to a color
    /// </summary>
    /// <param name="position">The coordinates in screen space</param>
    /// <param name="color">The pixel's color</param>
    private void SetPixel(Vector2i position, Vector3 color) {
        screen.pixels[position.Y * screen.width + position.X] = ShiftColor(color);
    }

    /// <summary>
    /// Compute the OpenGL integer format for the pixel
    /// </summary>
    /// <param name="color">The color</param>
    /// <returns>The color represented in an integer compatible with OpenGL</returns>
    private static int ShiftColor(Vector3 color) {
        var clamped = new Vector3i(
            (int)Math.Floor(Math.Clamp(color.X, 0f, 1f) * 255f),
            (int)Math.Floor(Math.Clamp(color.Y, 0f, 1f) * 255f),
            (int)Math.Floor(Math.Clamp(color.Z, 0f, 1f) * 255f));
        return ((byte)clamped.X << 16) | ((byte)clamped.Y << 8) | (byte)clamped.Z;
    }

    /// <summary>
    /// Mouse movement handler
    /// </summary>
    /// <param name="e">The event arguments</param>
    public void OnMouseMove(MouseMoveEventArgs e) {
        _yaw += e.DeltaX / 360;
        _pitch += e.DeltaY / 360;
    }
}