#version 330 core
out vec4 FragColor;

// PIs
#ifndef PI
#define PI 3.141592653589f
#endif

#ifndef ONE_OVER_PI
#define ONE_OVER_PI (1.0f / PI)
#endif

#ifndef ONE_OVER_TWO_PI
#define ONE_OVER_TWO_PI (1.0f / (2.0f * PI))
#endif

#ifndef ONE_OVER_FOUR_PI
#define ONE_OVER_FOUR_PI (1.0f / (4.0f * PI))
#endif

#ifndef ONE_OVER_EIGHT_PI
#define ONE_OVER_EIGHT_PI (1.0f / (8.0f * PI))
#endif

#define EPSILON 1e-5

#define LIGHT 0
#define SPHERE 1
#define PLANE 2

// Specular Type Codes
#define PHONG 0
#define BLINN_PHONG 1

// Diffuse Type Codes
#define LAMBERTIAN 0
#define OREN_NAYAR 1

struct Camera
{
    vec3 position;
    float near;
    float far;
};

struct Ray
{
    vec3 origin;
    vec3 direction;
};

struct Hit
{
    int obj_id;
    float dist;
};

struct Material
{
    vec3 color;
    float metalness;
    float roughness;
    float reflectance;
    float reflectivity;
};

struct Object
{
    int type;
    int index;
};

struct Light
{
    vec3 position;
    float radius;
    vec3 color;
    float power;
};

struct Plane
{
    vec3 normal;
    vec3 point;
    Material material;
};

struct Sphere
{
    vec3 center;
    float radius;
    Material material;
};

in vec4 FragPos;

uniform mat4 inv_view_proj;
uniform Camera cam;
uniform int specular_shader_type;
uniform int diffuse_shader_type;

vec3 ambient_color = vec3(1.0);
float ambient_intensity = 0.1;

const Material material_list[] = Material[]
(
    Material(vec3(1.0, 0.1, 0.1), 0.1, 0.8, 0.2, 0.01),
    Material(vec3(0.1, 1.0, 0.1), 0.1, 0.8, 0.2, 0.01),
    Material(vec3(0.1, 0.1, 1.0), 0.01, 0.6, 0.1, 0.01),
    Material(vec3(1.0), 0.5, 0.5, 0.5, 0.5)
);

const Object obj_list[] = Object[]
(
    Object(LIGHT, 0),
    Object(LIGHT, 1),
    Object(LIGHT, 2),
    Object(SPHERE, 0),
    Object(SPHERE, 1),
    Object(PLANE, 0),
    Object(PLANE, 1),
    Object(PLANE, 2),
    Object(PLANE, 3),
    Object(PLANE, 4),
    Object(PLANE, 5)
);

const Light light_list[] = Light[]
(
    Light(vec3(0.0, 1.0, 0.0), 0.25, vec3(1.0, 1.0, 1.0), 400.0),
    Light(vec3(-8.0, 8.0, -10.0), 0.25, vec3(0.0, 1.0, 1.0), 400.0),
    Light(vec3(8.0, 8.0, -10.0), 0.25, vec3(1.0, 0.0, 1.0), 400.0)
);

const Sphere sphere_list[] = Sphere[]
(
    Sphere(vec3(-3.0, -1.0, -7.0), 1.0, material_list[0]),
    Sphere(vec3(3.0, -1.0, -7.0), 1.0, material_list[1])
);

const Plane plane_list[] = Plane[]
(
    Plane(vec3(0.0, 1.0, 0.0), vec3(0.0, -2.0, 0.0), material_list[2]),
    Plane(vec3(1.0, 0.0, 0.0), vec3(-10.0, 0.0, 0.0), material_list[3]),
    Plane(vec3(-1.0, 0.0, 0.0), vec3(10.0, 0.0, 0.0), material_list[3]),
    Plane(vec3(0.0, 0.0, 1.0), vec3(0.0, 0.0, -15.0), material_list[3]),
    Plane(vec3(0.0, -1.0, 0.0), vec3(0.0, 10.0, 0.0), material_list[3]),
    Plane(vec3(0.0, 0.0, -1.0), vec3(0.0, 0.0, 15.0), material_list[3])
);

vec3 rayPoint(Ray ray, float t)
{
    return ray.origin + ray.direction * t;
}

float intersectLight(Ray ray, Light light)
{
    vec3 c_o = ray.origin - light.position;
    float b = dot(ray.direction, c_o);
    float c = dot(c_o, c_o) - light.radius * light.radius;
    float disc = b * b - c;

    if (disc >= 0.0)
    {
        return -b - sqrt(disc);
    }

    return -1.0; // No intersect
}

float intersectSphere(Ray ray, Sphere sphere)
{
    vec3 c_o = ray.origin - sphere.center;
    float b = dot(ray.direction, c_o);
    float c = dot(c_o, c_o) - sphere.radius * sphere.radius;
    float disc = b * b - c;

    if (disc >= 0.0)
    {
        return -b - sqrt(disc);
    }

    return -1.0; // No intersect
}

vec3 sphereNormal(vec3 point, Sphere sphere)
{
    return normalize(point - sphere.center);
}

float intersectPlane(Ray ray, Plane plane)
{
    float denominator = dot(ray.direction, plane.normal);

    if (abs(denominator) >= EPSILON)
    {
        return dot((plane.point - ray.origin), plane.normal) / denominator;
    }

    return -1.0; // No intersect
}

Hit closestHit(Ray ray, bool check_light)
{
    Hit closest_hit = Hit(-1, 1.0 / 0.0);

    for (int i = 0; i < obj_list.length(); i++)
    {
        float check_dist = -1;
        switch (obj_list[i].type)
        {
            case SPHERE:
                check_dist = intersectSphere(ray, sphere_list[obj_list[i].index]);
                break;
            case PLANE:
                check_dist = intersectPlane(ray, plane_list[obj_list[i].index]);
                break;
            case LIGHT:
                check_dist = check_light ? intersectLight(ray, light_list[obj_list[i].index]) : check_dist;
        }

        if (check_dist > EPSILON && check_dist < closest_hit.dist)
        {
            closest_hit.obj_id = i;
            closest_hit.dist = check_dist;
        }
    }

    return closest_hit;
}

vec3 fresnelSchlick(vec3 F0, vec3 N, vec3 V)
{
    return F0 + (vec3(1.0) - F0) * (1.0 - pow(dot(N, V), 5.0));
}

float orenNayarDiffuse(vec3 N, vec3 L, vec3 V, float sigma)
{
    float A = 1.0 - 0.5 * sigma / (sigma + 0.33);
    float B = 0.45 * sigma / (sigma + 0.09);

    vec3 proj_V = normalize(V - dot(V, N) * N);
    vec3 proj_L = normalize(L - dot(V, N) * N);
    float cos_phi_diff = max(0, dot(proj_V, proj_L));

    float theta_l = acos(dot(N, L));
    float theta_v = acos(dot(N, V));
    float alpha = max(theta_l, theta_v);
    float beta = min(theta_l, theta_v);

    return (A + B * cos_phi_diff) * sin(alpha) * tan(beta);
}

float phongSpecular(vec3 N, vec3 L, vec3 V, float shininess)
{
    float energy_conserve = (shininess + 2.0) * ONE_OVER_TWO_PI;
    vec3 R = reflect(-L, N);
    return energy_conserve * pow(max(0.0, dot(R, V)), shininess);
}

float blinnPhongSpecular(vec3 N, vec3 L, vec3 V, float shininess)
{
    float energy_conserve = ((shininess + 2.0) * (shininess + 4.0)) * ONE_OVER_EIGHT_PI / (pow(2.0, -shininess / 2.0) + shininess);
    vec3 H = normalize(L + V);
    return energy_conserve * pow(max(0.0, dot(H, N)), shininess);
}

vec3 shade(Ray ray, vec3 hit_point, vec3 normal, Material material, out vec3 reflectivity)
{
    vec3 result = vec3(0);

    vec3 to_view = -ray.direction;

    vec3 min_dielectric_F0 = vec3(0.16 * material.reflectance * material.reflectance);
    vec3 F0 = mix(min_dielectric_F0, material.color, material.metalness);
    reflectivity = F0 * F0;

    vec3 diffuse_reflectance = material.color * (1.0 - material.metalness);
    vec3 fresnel_adjust = 1 - fresnelSchlick(F0, normal, to_view);

    // ambient
    result = material.color * ambient_color * ambient_intensity;

    for (int i = 0; i < light_list.length(); i++)
    {
        vec3 to_light = normalize(light_list[i].position - hit_point);
        float to_light_dist = distance(light_list[i].position, hit_point);

        bool inside_shadow = false;

        Hit check_occlusion = closestHit(Ray(hit_point, to_light), false);

        if (check_occlusion.obj_id != -1 && check_occlusion.dist < to_light_dist)
        {
            inside_shadow = true;
        }

        if (!inside_shadow)
        {
            float to_light_dist2 = max(EPSILON, to_light_dist * to_light_dist);

            // light intensity
            vec3 light_intensity = light_list[i].color * light_list[i].power * ONE_OVER_FOUR_PI / to_light_dist2;

            // beckmann roughness
            float alpha = max(material.roughness * material.roughness, EPSILON);

            // specular
            vec3 specular = vec3(0);
            float k_spec;

            if (specular_shader_type == PHONG)
            {
                float shininess = 2.0 / clamp(alpha * alpha, EPSILON, 1.0 - EPSILON) - 2.0;
                k_spec = phongSpecular(to_light, normal, -ray.direction, shininess);
            }
            else if (specular_shader_type == BLINN_PHONG)
            {
                float shininess = 2.0 / clamp(alpha * alpha, EPSILON, 1.0 - EPSILON) - 2.0;
                shininess *= 4.0;
                k_spec = blinnPhongSpecular(to_light, normal, -ray.direction, shininess);
            }

            specular = k_spec * F0;

            // diffuse
            vec3 diffuse = vec3(0);
            float k_diff = 1.0; // default: 1.0 is Lambertian

            if (diffuse_shader_type == OREN_NAYAR)
            {
                // beckmann roughness to oren-nayar roughness
                float sigma = 0.7071068 * atan(alpha);
                k_diff = orenNayarDiffuse(normal, to_light, -ray.direction, sigma);
            }

            diffuse = k_diff * diffuse_reflectance * ONE_OVER_PI * fresnel_adjust;

            result += (diffuse + specular) * light_intensity * max(0.0, dot(normal, to_light));
        }
    }

    return result;
}

vec3 castRay(Ray ray)
{
    vec3 result = vec3(0);

    Ray curr_ray = ray;
    vec3 reflect_mult = vec3(1.0);

    for (int i = 0; i < 5; i++)
    {
        Hit closest_hit = closestHit(curr_ray, true);

        if (closest_hit.obj_id != -1 && closest_hit.dist > cam.near && closest_hit.dist < cam.far)
        {
            if (obj_list[closest_hit.obj_id].type == LIGHT)
            {
                vec3 light_intensity = light_list[closest_hit.obj_id].color * light_list[closest_hit.obj_id].power / (i + 1);
                result += light_intensity * reflect_mult;
                break;
            }

            vec3 hit_point = rayPoint(curr_ray, closest_hit.dist);
            vec3 normal;
            Material material;
            switch (obj_list[closest_hit.obj_id].type)
            {
                case SPHERE:
                    normal = sphereNormal(hit_point, sphere_list[obj_list[closest_hit.obj_id].index]);
                    material = sphere_list[obj_list[closest_hit.obj_id].index].material;
                    break;
                case PLANE:
                    normal = plane_list[obj_list[closest_hit.obj_id].index].normal;
                    material = plane_list[obj_list[closest_hit.obj_id].index].material;
            }

            if (dot(curr_ray.direction, normal) > 0)
            {
                normal *= -1.0;
            }

            vec3 reflectivity = vec3(0);

            result += shade(curr_ray, hit_point, normal, material, reflectivity) * reflect_mult;

            if (length(reflectivity) > EPSILON)
            {
                vec3 reflect_vec = reflect(curr_ray.direction, normal);
                curr_ray = Ray(hit_point, reflect_vec);
                reflect_mult *= reflectivity;
            }
            else
            {
                break;
            }
        }
    }

    return result;
}

void main()
{
    vec4 worldPos = inv_view_proj * FragPos;
    vec3 pixelPos = worldPos.xyz / worldPos.w;

    vec3 rayDir = normalize(pixelPos - cam.position);

    Ray ray = Ray(cam.position, rayDir);

    FragColor = vec4(castRay(ray), 0);
}