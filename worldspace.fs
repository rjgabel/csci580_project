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

#define SPHERE 0
#define PLANE 1

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
};

struct Object
{
    int type;
    int index;
};

struct Light
{
    vec3 position;
    vec3 color;
    float power;
};

struct Plane
{
    vec3 normal;
    vec3 point;
    Material material;
    float reflectivity;
};

struct Sphere
{
    vec3 center;
    float radius;
    Material material;
    float reflectivity;
};

in vec4 FragPos;

uniform mat4 inv_view_proj;
uniform Camera cam;
uniform int specular_shader_type;
uniform int diffuse_shader_type;

vec3 ambient_color = vec3(1.0);
float ambient_intensity = 0.05;

const Material material_list[] = Material[]
(
    Material(vec3(1.0, 0.1, 0.1), 0.2, 0.6, 0.5),
    Material(vec3(0.1, 1.0, 0.1), 0.2, 0.6, 0.5),
    Material(vec3(0.1, 0.1, 1.0), 0.8, 0.5, 0.5)
);

const Object obj_list[] = Object[]
(
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
    Light(vec3(0.0, 1.0, 0.0), vec3(1.0, 1.0, 1.0), 400.0),
    Light(vec3(-8.0, 8.0, -10.0), vec3(1.0, 1.0, 1.0), 400.0),
    Light(vec3(8.0, 8.0, -10.0), vec3(1.0, 1.0, 1.0), 400.0)
);

const Sphere sphere_list[] = Sphere[]
(
    Sphere(vec3(-3.0, -1.0, -7.0), 1.0, material_list[0], 0.01),
    Sphere(vec3(3.0, -1.0, -7.0), 1.0, material_list[1], 0.01)
);

const Plane plane_list[] = Plane[]
(
    Plane(vec3(0.0, 1.0, 0.0), vec3(0.0, -2.0, 0.0), material_list[2], 0.5),
    Plane(vec3(1.0, 0.0, 0.0), vec3(-10.0, 0.0, 0.0), material_list[2], 0.5),
    Plane(vec3(-1.0, 0.0, 0.0), vec3(10.0, 0.0, 0.0), material_list[2], 0.5),
    Plane(vec3(0.0, 0.0, 1.0), vec3(0.0, 0.0, -15.0), material_list[2], 0.5),
    Plane(vec3(0.0, -1.0, 0.0), vec3(0.0, 10.0, 0.0), material_list[2], 0.5),
    Plane(vec3(0.0, 0.0, -1.0), vec3(0.0, 0.0, 15.0), material_list[2], 0.5)
);

vec3 rayPoint(Ray ray, float t)
{
    return ray.origin + ray.direction * t;
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

Hit closestHit(Ray ray)
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
        }

        if (check_dist > EPSILON && check_dist < closest_hit.dist)
        {
            closest_hit.obj_id = i;
            closest_hit.dist = check_dist;
        }
    }

    return closest_hit;
}

vec3 lambertianDiffuse(vec3 N, vec3 L, vec3 diffuse_reflectance, vec3 light_color)
{
    return diffuse_reflectance * ONE_OVER_PI * max(0.0, dot(N, L)) * light_color;
}

vec3 orenNayarDiffuse(vec3 N, vec3 L, vec3 V, vec3 diffuse_reflectance, float sigma, vec3 light_color)
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
    
    float k = (A + B * cos_phi_diff) * sin(alpha) * tan(beta) * max(0.0, dot(L, N));

    vec3 result = diffuse_reflectance * ONE_OVER_PI * k * light_color;
    return result;
}

vec3 phongSpecular(vec3 N, vec3 L, vec3 V, vec3 spec_f0, vec3 light_color, float shininess)
{
    float energy_conserve = (shininess + 2.0) * ONE_OVER_TWO_PI;
    vec3 R = reflect(-L, N);
    float k = pow(max(0.0, dot(R, V)), shininess) * max(0.0, dot(L, N));
    vec3 result = energy_conserve * k * spec_f0 * light_color;
    return result;
}

vec3 blinnPhongSpecular(vec3 N, vec3 L, vec3 V, vec3 spec_f0, vec3 light_color, float shininess)
{
    float energy_conserve = ((shininess + 2.0) * (shininess + 4.0)) * ONE_OVER_EIGHT_PI / (pow(2.0, -shininess / 2.0) + shininess);
    vec3 H = normalize(L + V);
    float k = pow(max(0.0, dot(H, N)), shininess) * max(0.0, dot(L, N));
    vec3 result = energy_conserve * k * spec_f0 * light_color;
    return result;
}

vec3 shade(Ray ray, vec3 hit_point, vec3 normal, Material material)
{
    vec3 result = vec3(0);

    // ambient
    result = material.color * ambient_color * ambient_intensity;

    for (int i = 0; i < light_list.length(); i++)
    {
        vec3 to_light = normalize(light_list[i].position - hit_point);
        float to_light_dist = distance(light_list[i].position, hit_point);

        bool inside_shadow = false;

        Hit check_occlusion = closestHit(Ray(hit_point, to_light));

        if (check_occlusion.obj_id != -1 && check_occlusion.dist < to_light_dist)
        {
            inside_shadow = true;
        }

        if (!inside_shadow)
        {
            float to_light_dist2 = max(EPSILON, to_light_dist * to_light_dist);

            // light intensity
            vec3 light_color = light_list[i].color * light_list[i].power * ONE_OVER_FOUR_PI / to_light_dist2;

            // beckmann roughness
            float alpha = material.roughness * material.roughness;

            // specular
            vec3 specular;
            vec3 min_dielectric_F0 = vec3(0.16f * material.reflectance * material.reflectance);
            vec3 spec_F0 = mix(min_dielectric_F0, material.color, material.metalness);
            if (specular_shader_type == PHONG)
            {
                float shininess = 2.0 / min(1 - EPSILON, max(EPSILON, alpha * alpha)) - 2.0;
                specular = phongSpecular(to_light, normal, -ray.direction, spec_F0, light_color, shininess);
            }
            else if (specular_shader_type == BLINN_PHONG)
            {
                float shininess = 2.0 / min(1 - EPSILON, max(EPSILON, alpha * alpha)) - 2.0;
                shininess *= 4.0;
                specular = blinnPhongSpecular(to_light, normal, -ray.direction, spec_F0, light_color, shininess);
            }
            result += specular;

            // diffuse
            vec3 diffuse;
            vec3 diffuse_reflectance = material.color * (1.0 - material.metalness);
            if (diffuse_shader_type == LAMBERTIAN)
            {
                diffuse = lambertianDiffuse(normal, to_light, diffuse_reflectance, light_color);
            }
            else if (diffuse_shader_type == OREN_NAYAR)
            {
                // beckmann roughness to oren-nayar roughness
                float sigma = 0.7071068 * atan(alpha);
                diffuse = orenNayarDiffuse(normal, to_light, -ray.direction, diffuse_reflectance, sigma, light_color);
            }
            result += diffuse;
        }
    }


    return result;
}

vec3 castRay(Ray ray)
{
    vec3 result = vec3(0);

    Ray curr_ray = ray;
    float reflect_mult = 1.0;

    for (int i = 0; i < 5; i++)
    {
        Hit closest_hit = closestHit(curr_ray);

        if (closest_hit.obj_id != -1 && closest_hit.dist > cam.near && closest_hit.dist < cam.far)
        {
            vec3 hit_point = rayPoint(curr_ray, closest_hit.dist);
            vec3 normal;
            Material material;
            float reflectivity = 0;
            switch (obj_list[closest_hit.obj_id].type)
            {
                case SPHERE:
                    normal = sphereNormal(hit_point, sphere_list[obj_list[closest_hit.obj_id].index]);
                    material = sphere_list[obj_list[closest_hit.obj_id].index].material;
                    reflectivity = sphere_list[obj_list[closest_hit.obj_id].index].reflectivity;
                    break;
                case PLANE:
                    normal = plane_list[obj_list[closest_hit.obj_id].index].normal;
                    material = plane_list[obj_list[closest_hit.obj_id].index].material;
                    reflectivity = plane_list[obj_list[closest_hit.obj_id].index].reflectivity;
                    break;
            }

            if (dot(curr_ray.direction, normal) > 0)
            {
                normal *= -1.0;
            }

            vec3 part_result = vec3(0);

            if (reflectivity < 1.0)
            {
                part_result = shade(curr_ray, hit_point, normal, material) * reflect_mult;
            }

            if (reflectivity > 0.0)
            {
                part_result *= (1 - reflectivity);

                vec3 reflect_vec = reflect(curr_ray.direction, normal);
                curr_ray = Ray(hit_point, reflect_vec);
                reflect_mult *= reflectivity;

                result += part_result;
            }
            else
            {
                result += part_result;
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