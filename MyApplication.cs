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
    
    private Material(Vector3 diffuseColor, Vector3 ambientColor, Vector3 specularColor, float specularity, Vector3 mirrorColor) {
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

    public TraceResult(float distance, Vector3 color) {
        this.distance = distance;
        this.color = color;
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

    public Plane(Vector3 center, Vector3 normal) {
        this.center = center;
        this.normal = normal;
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
        this.diameter = radius * 2;
        this.radiusSquared = radius * radius;
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

internal struct TracedRay {
    public readonly Ray ray;
    public readonly IntersectResult intersectResult;

    public TracedRay(Ray ray, IntersectResult intersectResult) {
        this.ray = ray;
        this.intersectResult = intersectResult;
    }
}

internal class MyApplication {
    private readonly Sphere[] _spheres = {
        new(new Vector3(2.5f, 0, 5), 1.0f, Material.Diffuse(new Vector3(1f, 0, 0))),
        new(new Vector3(3, 0, 8), 1.0f, Material.Plastic(new Vector3(0, 1, 0))),
        new(new Vector3(-3, 0, 8), 1.0f, Material.Diffuse(new Vector3(0, 0, 1))),
    };

    private readonly Light[] _lights = {
        new(new Vector3(-3, 1 ,-3), 1f),
    };

    private readonly Plane[] _planes = {
        new(new Vector3(0, 0, 0), new Vector3(0, 1, 0)),
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
    private static readonly Vector3 CameraPosition = new(0.0f, 0.0f, 0.0f);
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
        Vector3.Cross(CameraForwardDirection, CameraRightDirection);

    private List<TracedRay> _tracedRays = new();
    
    public MyApplication(Surface screen) {
        this.screen = screen;
    }

    /// <summary>
    /// Set the yaw and pitch such that the camera looks at the provided destination vector.
    /// </summary>
    /// <param name="dest">The destination vector to look at</param>
    private void LookAt(Vector3 dest) {
        Vector3 destNormal = Vector3.Normalize(dest);
        _yaw = (float) Math.Atan2(destNormal.X, destNormal.Z);
        _pitch = (float) Math.Asin(-destNormal.Y);
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
        foreach(Sphere sphere in _spheres) {
            if (IntersectsSphere(ray, sphere, 0.001f).collision) {
                intersectsObstruction = true;
            }
        }

        if (intersectsObstruction) {
            return 0.0f;
        }

        return light.intensity;
    }

    /// <summary>
    /// Intersect a ray with a plane
    /// </summary>
    /// <param name="ray">The ray to intersect</param>
    /// <param name="plane">The plane to intersect with</param>
    /// <returns>The result of the trace</returns>
    private IntersectResult IntersectPlane(Ray ray, Plane plane) {
        Vector3 offsetCenter = Vector3.Normalize(plane.center - ray.origin);
        float denominator = Vector3.Dot(plane.normal, ray.direction);
        if (Math.Abs(denominator) > 0.001f) {
            float t = Vector3.Dot(offsetCenter, plane.normal);
            if (t >= 0) {
                Vector3 planeNormalNormalized = Vector3.Normalize(plane.normal);
                float distance = (float) Math.Sqrt(Vector3.Dot(planeNormalNormalized, planeNormalNormalized));

                _tracedRays.Add(new TracedRay(ray, IntersectResult.Collide(distance)));
                return IntersectResult.Collide(distance); 
            }
        }
        
        _tracedRays.Add(new TracedRay(ray, IntersectResult.Miss()));
        return IntersectResult.Miss();
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
            float dSqrt = (float) Math.Sqrt(d);
            float a2 = 2 * a;
            
            float distance2 = (-b + dSqrt) / a2;
            float distance1 = (-b - dSqrt) / a2;

            float d1Eps = distance1 - epsilon;
            float d2Eps = distance2 - epsilon;

            float distance = Math.Min(Math.Max(distance1, 0), Math.Max(distance2, 0));
            float distanceEps = Math.Min(Math.Max(d1Eps, 0), Math.Max(d2Eps, 0));
            
            intersectResult = distanceEps > 0 ? IntersectResult.Collide(distance) : IntersectResult.Miss();
            
        }

        _tracedRays.Add(new TracedRay(ray, intersectResult));
        return intersectResult;
    }

    /// <summary>
    /// Compute the light for a point in space resulting from the phong shading model
    /// </summary>
    /// <param name="hitPoint">The point to compute for</param>
    /// <param name="primaryRay">The primary (view) ray that hit the point</param>
    /// <param name="sphere">The sphere the point lies on</param>
    /// <param name="light">The light source to calculate for</param>
    /// <returns>The RGB color</returns>
    private static Vector3 ComputePhongShading(Vector3 hitPoint, Ray primaryRay, Sphere sphere, Light light) {
        Vector3 surfaceNormal = Vector3.Normalize(hitPoint - sphere.center);
        Vector3 lightDirectionNormal = Vector3.Normalize(light.position - hitPoint);
        Vector3 viewNormal = Vector3.Normalize(primaryRay.direction);
        
        Vector3 diffuseColor = Vector3.Zero;
        if (sphere.material.IsDiffuse) {
            float angle = Vector3.Dot(
                surfaceNormal,
                lightDirectionNormal
            );

            diffuseColor = sphere.material.diffuseColor // Kd
                           * Math.Max(0, angle); // max(0, N * L)
        }
        
        Vector3 specularColor = Vector3.Zero;
        if (sphere.material.HasSpecularity) {
            Vector3 specularDirection = lightDirectionNormal - 2 * Vector3.Dot(lightDirectionNormal, surfaceNormal) * surfaceNormal;
            float specularity = Vector3.Dot(
                viewNormal,
                Vector3.Normalize(specularDirection)
            );
        
            specularColor = sphere.material.specularColor // Kd
                            * VecUtil.FromFloat3((float) Math.Pow(Math.Max(0, specularity), sphere.material.specularity));   
        }

        return diffuseColor + specularColor;
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

    private Vector3 TracePlane(Ray ray, Plane plane) {
        IntersectResult intersectResult = IntersectPlane(ray, plane);
        if (intersectResult.collision && intersectResult.distance >= 0) {
            return Vector3.One;
        }
        
        return Vector3.Zero;
    }
    
    private TraceResult TraceSphere(Ray ray, Sphere sphere, int secondaryBounceCount = 0) {
        IntersectResult intersectResult = IntersectsSphere(ray, sphere);
        
        // No intersections for this ray
        if (!intersectResult.collision || intersectResult.distance <= 0) {
            return new TraceResult(intersectResult.distance, Vector3.Zero);   
        }

        // Too many secondary ray bounces
        if (secondaryBounceCount > 32) {
            return new TraceResult(intersectResult.distance, Vector3.Zero);
        }
        
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

            // Trace to all other spheres
            foreach (Sphere secondarySphere in _spheres) {
                color += TraceSphere(
                    Ray.Secondary(hitPoint, secondaryRayDirection), 
                    secondarySphere,
                    secondaryBounceCount
                ).color;
            }

            color *= sphere.material.mirrorColor;
        }

        // Compute phong shading
        if (sphere.material.IsDiffuse) {
            foreach (Light light in _lights) {
                float lightIntensity = IntersectShadowLight(hitPoint, light);
                Vector3 intensityRgb = VecUtil.FromFloat3(lightIntensity);
                float distanceAttenuation = 1 / sphere.radiusSquared;
                
                color +=
                    (intensityRgb * distanceAttenuation * ComputePhongShading(hitPoint, ray, sphere, light))
                    .Max(0.0f); // Make sure the color stays positive
            }   
        }

        // Add ambient light
        color += _ambientLightColor * sphere.material.ambientColor;

        return new TraceResult(intersectResult.distance, color);
    }

    private bool _hasTicked;
    
    public void Tick() {
        // TODO Temporary run only once.
        if (_hasTicked) return;
        _hasTicked = true;
        
        screen.Clear(0);

        float planeHeight = NearClip * (float)Math.Tan(MathHelper.DegreesToRadians(FieldOfView * 0.5f)) * 2;
        float aspectRatio = (float)screen.width / screen.height;
        float planeWidth = planeHeight * aspectRatio;

        var viewParams = new Vector3(planeWidth, planeHeight, NearClip);

        int debugHeight = (int) Math.Floor(screen.height * DebugSizeScaler);
        int debugWidth = (int) Math.Floor(screen.width * DebugSizeScaler);

        int debugTopLeftX = screen.width - debugWidth;
        int debugTopLeftY = screen.height - debugHeight;
        Console.WriteLine($"Debug top left ({debugTopLeftX}, {debugTopLeftY})");
        bool IsInDebug(int x, int y) => x >= debugTopLeftX && y >= debugTopLeftY; 

        for (int x = 0; x < screen.width; x++) {
            for (int y = 0; y < screen.height; y++) {
                Vector2 vplXy = new Vector2(x, y) / new Vector2(screen.width, screen.height) -
                                VecUtil.FromFloat2(0.5f);
                Vector3 viewPointLocal = new Vector3(vplXy.X, vplXy.Y, 1f) * viewParams;

                Vector3 viewPoint = CameraPosition + CameraRightDirection * viewPointLocal.X +
                                    CameraUpDirection * viewPointLocal.Y +
                                    CameraForwardDirection * viewPointLocal.Z;

                var cameraPrimaryRay = Ray.Primary(CameraPosition, Vector3.Normalize(viewPoint - CameraPosition));

                Vector3 pixelColor = Vector3.Zero;
                float nearestDist = float.PositiveInfinity;
                foreach (Sphere sphere in _spheres) {
                    TraceResult result = TraceSphere(cameraPrimaryRay, sphere);
                    if (result.distance > 0 && nearestDist > result.distance) {
                        nearestDist = result.distance;
                        pixelColor = result.color;
                    }
                }

                foreach (Plane plane in _planes) {
                    pixelColor += TracePlane(cameraPrimaryRay, plane);
                }

                if (!IsInDebug(x, y)) {
                    SetPixel(new Vector2i(x, y), pixelColor);
                }
            }
        }

        float mostLeft = _spheres.Min(f => f.center.X + f.radius);
        float mostRight = _spheres.Max(f => f.center.X + f.radius);
        float usedWidth = mostRight - mostLeft;

        float mostTop = _spheres.Min(f => f.center.Z + f.radius);
        float mostBottom = _spheres.Max(f => f.center.Z + f.radius);
        float usedHeight = mostBottom - mostTop;
        
        foreach (Sphere sphere in _spheres) {
            float distToLeft = mostLeft - sphere.center.X;
            float distToTop = mostTop - sphere.center.Z;
            
            Vector2i center = new(
                (int) distToLeft + debugTopLeftX,
                (int) distToTop + debugTopLeftY
            );
            float scaledRadius = sphere.radius / DebugSizeScaler;
            Console.WriteLine($"Drawing circle at {center} with radius {scaledRadius}");
            DrawCircle(center, scaledRadius);
        }
        
        foreach (TracedRay tracedRay in _tracedRays) {
            
        }
    }

    private void DrawCircle(Vector2i center, float radius) {
        for (int angle = 0; angle < 360; angle++) {
            float angleRad = MathHelper.DegreesToRadians(angle);
            int x = (int) (center.X + radius * Math.Cos(angleRad));
            int y = (int) (center.Y + radius * Math.Sin(angleRad));
            
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
}