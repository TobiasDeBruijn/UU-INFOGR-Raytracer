using OpenTK.Mathematics;

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

    public static Vector3d ToDouble(this Vector3 source) {
        return new Vector3d(
            source.X,
            source.Y,
            source.Z
        );
    }

    public static Vector3 ToFloat(this Vector3d source) {
        return new Vector3(
            (float) source.X,
            (float) source.Y,
            (float) source.Z
        );
    }

    public static double StableAngle(Vector3d left, Vector3d right) {
        Vector3d leftDiv = left / left.Length;
        Vector3d rightDiv = right / right.Length;

        double x = (leftDiv + rightDiv).Length;
        double y = (leftDiv - rightDiv).Length;

        return Math.Atan2(x, y);
    }

    public static bool IsZero(this Vector3 vector) {
        return vector.X == 0 && vector.Y == 0 && vector.Z == 0;
    }
}

internal struct Material {
    public readonly Vector3 DiffuseColor;
    public readonly Vector3 AmbientColor;
    public readonly Vector3 SpecularColor;
    public readonly float Specularity;
    public readonly Vector3 MirrorColor;

    private Material(Vector3 diffuseColor, Vector3 ambientColor, Vector3 specularColor, float specularity, Vector3 mirrorColor) {
        this.DiffuseColor = diffuseColor;
        this.AmbientColor = ambientColor;
        this.SpecularColor = specularColor;
        this.Specularity = specularity;
        MirrorColor = mirrorColor;
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
}

internal struct TraceResult {
    public readonly bool Collision;
    public readonly float Distance;

    private TraceResult(bool collision, float distance) {
        this.Collision = collision;
        this.Distance = distance;
    }

    public static TraceResult Collide(float distance) {
        return new TraceResult(true, distance);
    }

    public static TraceResult Miss() {
        return new TraceResult(false, 0f);
    }
}

internal struct Light {
    public readonly Vector3 Position;
    public readonly float Intensity;

    public Light(Vector3 position, float intensity) {
        this.Position = position;
        this.Intensity = intensity;
    }
}

internal struct Sphere {
    public readonly Vector3 Center;
    public readonly float Radius;
    public readonly Material Material;
    public readonly float Diameter;
    public readonly float RadiusSquared;

    public Sphere(Vector3 center, float radius, Material material) {
        this.Center = center;
        this.Radius = radius;
        this.Material = material;
        this.Diameter = radius * 2;
        this.RadiusSquared = radius * radius;
    }
}

internal struct Ray {
    public Vector3 Origin;
    public Vector3 Direction;

    public Ray(Vector3 origin, Vector3 direction) {
        this.Origin = origin;
        this.Direction = direction;
    }
}

internal class MyApplication {
    private readonly Sphere[] _spheres = {
        new(new Vector3(0, 0, 8), 1.0f, Material.Mirror(new Vector3(1, 0, 0))),
        new(new Vector3(3, 0, 8), 1.0f, Material.Plastic(new Vector3(0, 1, 0))),
        new(new Vector3(-3, 0, 8), 1.0f, Material.Diffuse(new Vector3(0, 0, 1))),
    };

    private readonly Light[] _lights = {
        new(new Vector3(-5, 0, 0), 1f),
        // new(new Vector3(5, 9, 0), 0.7f),
    };

    private readonly Vector3 _ambientLightColor = VecUtil.FromFloat3(43f / 255f);
    
    private const float NearClip = 0.3f;
    private const float FieldOfView = 60f;

    private static readonly Vector3 CameraPosition = new(0.0f, 0.0f, 0.0f);

    private const float Yaw = 0;
    private const float Pitch = 0;

    // Source: https://gamedev.stackexchange.com/a/190058
    private static readonly Vector3 CameraForwardDirection = new((float)(Math.Cos(Pitch) * Math.Sin(Yaw)),
        (float)-Math.Sin(Pitch), (float)(Math.Cos(Pitch) * Math.Cos(Yaw)));

    private static readonly Vector3 CameraRightDirection = new((float)Math.Cos(Yaw), 0, (float)-Math.Sin(Yaw));
    private static readonly Vector3 CameraUpDirection = Vector3.Cross(CameraForwardDirection, CameraRightDirection);

    public readonly Surface Screen;

    public MyApplication(Surface screen) {
        this.Screen = screen;
    }

    /// <summary>
    /// Returns the intensity of the light at the provided hit point from the provided light.
    /// Does not take distance attenuation into account.
    /// </summary>
    /// <param name="hitPoint">The point to calculate the intensity for</param>
    /// <param name="light">The light to check from</param>
    /// <returns>The intensity of the light if there's an unobstructed path</returns>
    private float IntersectShadowLight(Vector3 hitPoint, Light light) {
        Ray ray = new Ray(hitPoint, light.Position);

        bool intersectsObstruction = false;
        foreach(Sphere sphere in _spheres) {
            if (IntersectsSphere(ray, sphere, 0.001f).Collision) {
                intersectsObstruction = true;
            }
        }

        if (intersectsObstruction) {
            return 0.0f;
        }

        return light.Intensity;
    }

    private static TraceResult IntersectsSphere(Ray ray, Sphere sphere, float epsilon = 0.0f) {
        Vector3 offsetOrigin = ray.Origin - sphere.Center;

        float a = Vector3.Dot(ray.Direction, ray.Direction);
        float b = 2 * Vector3.Dot(offsetOrigin, ray.Direction);
        float c = Vector3.Dot(offsetOrigin, offsetOrigin) - sphere.RadiusSquared;
        
        float d = b * b - 4 * a * c;
        if (d >= 0) {
            float dSqrt = (float) Math.Sqrt(d);
            float a2 = 2 * a;
            
            float distance2 = (-b + dSqrt) / a2;
            float distance1 = (-b - dSqrt) / a2;

            float d1Eps = distance1 - epsilon;
            float d2Eps = distance2 - epsilon;

            float distance = Math.Min(Math.Max(distance1, 0), Math.Max(distance2, 0));
            float distanceEps = Math.Min(Math.Max(d1Eps, 0), Math.Max(d2Eps, 0));
            
            return distanceEps > 0 ? TraceResult.Collide(distance) : TraceResult.Miss();
        }

        return TraceResult.Miss();
    }

    private static Vector3 ComputePhongShading(Vector3 hitPoint, Ray primaryRay, Sphere sphere, Light light) {
        Vector3 surfaceNormal = Vector3.Normalize(hitPoint - sphere.Center);
        Vector3 lightDirectionNormal = Vector3.Normalize(light.Position - hitPoint);
        Vector3 viewNormal = Vector3.Normalize(primaryRay.Direction);
        
        float angle = Vector3.Dot(
            surfaceNormal,
            lightDirectionNormal
        );

        Vector3 diffuseColor =
            sphere.Material.DiffuseColor // Kd
            * Math.Max(0, angle); // max(0, N * L)

        Vector3 specularDirection = lightDirectionNormal - 2 * Vector3.Dot(lightDirectionNormal, surfaceNormal) * surfaceNormal;
        float specularity = Vector3.Dot(
            viewNormal,
            Vector3.Normalize(specularDirection)
        );
        
        Vector3 specularColor =
            sphere.Material.SpecularColor // Kd
            * VecUtil.FromFloat3((float) Math.Pow(Math.Max(0, specularity), sphere.Material.Specularity));

        return diffuseColor + specularColor;
    }

    private static Vector3 CalculateReflectionRay(Vector3 viewRay, Vector3 normalRay) {
        return viewRay - 2 * Vector3.Dot(viewRay, normalRay) * normalRay;
    }

    private Vector3 IntersectSphere(Ray ray, Sphere sphere, int secondaryBounceCount = 0) {
        TraceResult traceResult = IntersectsSphere(ray, sphere);
        
        // At least one intersection
        if (traceResult.Collision && traceResult.Distance >= 0) {
            // Point on the surfac of the sphere
            Vector3 hitPoint = ray.Origin + ray.Direction * traceResult.Distance;
            Vector3 color = Vector3.Zero;

            if (sphere.Material.DiffuseColor.IsZero() && !sphere.Material.MirrorColor.IsZero()) {
                if (secondaryBounceCount > 32) {
                    return Vector3.Zero;
                }
                
                Vector3 secondaryRayDirection = CalculateReflectionRay(
                    ray.Direction.Normalized(),
                    (hitPoint - sphere.Center).Normalized()
                );

                Vector3 secondaryRayColor = Vector3.Zero;
                // Trace to all other spheres
                foreach (Sphere secondarySphere in _spheres) {
                    Console.WriteLine(secondaryBounceCount);
                    secondaryRayColor += IntersectSphere(
                        new Ray(hitPoint, secondaryRayDirection), 
                        secondarySphere,
                        secondaryBounceCount == 0 ? 1 : ++secondaryBounceCount
                    );
                    
                    Console.WriteLine(secondaryRayColor);
                }

                color = secondaryRayColor * sphere.Material.SpecularColor;
            } else {
                // Compute the contributions from each light
                foreach (Light light in _lights) {
                    float lightIntensity = IntersectShadowLight(hitPoint, light);
                    Vector3 intensityRgb = VecUtil.FromFloat3(lightIntensity);
                    float distanceAttenuation = 1 / sphere.RadiusSquared;
                
                    color +=
                        (intensityRgb * distanceAttenuation * ComputePhongShading(hitPoint, ray, sphere, light))
                        .Max(0.0f); // Make sure the color stays positive
                }
            }
            
            // Add ambient light
            color += _ambientLightColor * sphere.Material.AmbientColor;

            return color;
        }

        return Vector3.Zero;
    }

    public void Tick() {
        Screen.Clear(0);

        float planeHeight = NearClip * (float)Math.Tan(MathHelper.DegreesToRadians(FieldOfView * 0.5f)) * 2;
        float aspectRatio = (float)Screen.width / Screen.height;
        float planeWidth = planeHeight * aspectRatio;

        var viewParams = new Vector3(planeWidth, planeHeight, NearClip);

        for (int x = 0; x < Screen.width; x++) {
            for (int y = 0; y < Screen.height; y++) {
                Vector2 vplXy = new Vector2(x, y) / new Vector2(Screen.width, Screen.height) -
                                new Vector2(0.5f, 0.5f);
                Vector3 viewPointLocal = new Vector3(vplXy.X, vplXy.Y, 1f) * viewParams;

                Vector3 viewPoint = CameraPosition + CameraRightDirection * viewPointLocal.X +
                                    CameraUpDirection * viewPointLocal.Y +
                                    CameraForwardDirection * viewPointLocal.Z;

                var cameraPrimaryRay = new Ray(CameraPosition, Vector3.Normalize(viewPoint - CameraPosition));

                Vector3 pixelColor = Vector3.Zero;
                foreach (Sphere sphere in _spheres) {
                    pixelColor += IntersectSphere(cameraPrimaryRay, sphere);
                }
                
                SetPixel(new Vector2i(x, y), pixelColor);
            }
        }
    }

    private void SetPixel(Vector2i position, Vector3 color) {
        Screen.pixels[position.Y * Screen.width + position.X] = ShiftColor(color);
    }

    private int ShiftColor(Vector3 color) {
        var clamped = new Vector3i(
            (int)Math.Floor(Math.Clamp(color.X, 0f, 1f) * 255f), 
            (int)Math.Floor(Math.Clamp(color.Y, 0f, 1f) * 255f),
            (int)Math.Floor(Math.Clamp(color.Z, 0f, 1f) * 255f));
        return ((byte)clamped.X << 16) | ((byte)clamped.Y << 8) | (byte)clamped.Z;
    }
}