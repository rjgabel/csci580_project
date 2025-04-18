#version 330 core
out vec4 FragColor;

// PIs
#ifndef PI
#define PI 3.141592653589f
#endif

#ifndef TWO_PI
#define TWO_PI (2.0f * PI)
#endif

#ifndef ONE_OVER_PI
#define ONE_OVER_PI (1.0f / PI)
#endif

#ifndef ONE_OVER_TWO_PI
#define ONE_OVER_TWO_PI (1.0f / TWO_PI)
#endif

#define SPHERE 0
#define PLANE 1

#define EPSILON 1e-4

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

// struct Material
// {
//     vec3 ambient;
//     vec3 diffuse;
//     vec3 specular;
//     float shininess;
// };

struct PointLight
{
    vec3 position;
    vec3 color;
};

struct Object
{
    int type;
    int index;
};

struct Plane
{
    vec3 normal;
    vec3 point;
    vec3 color;
    float reflectivity;
};

struct Sphere
{
    vec3 center;
    float radius;
    vec3 color;
    float reflectivity;
};

const Object obj_list[] = Object[]
(
    Object(SPHERE, 0),
    Object(SPHERE, 1),
    Object(PLANE, 0)
);

const Sphere sphere_list[] = Sphere[]
(
    Sphere(vec3(0, -1.0, -7.0), 1.0, vec3(1.0, 0.0, 0.0), 0.0),
    Sphere(vec3(3.0, -1.0, -7.0), 1.0, vec3(1.0, 1.0, 0.0), 0.0)
);

const Plane plane_list[] = Plane[]
(
    Plane(vec3(0.0, 1.0, 0.0), vec3(0.0, -2.0, 0.0), vec3(0.0, 0.1, 0.8), 0.5)
);

PointLight pl = PointLight(vec3(-4.0, 1.0, 0.0), vec3(1.0, 1.0, 1.0));

vec3 ambient_color = vec3(1.0);
float ambient_intensity = 0.2;

in vec4 FragPos;

uniform mat4 inv_view_proj;
uniform Camera cam;
uniform int specular_shader_type;

float length2(vec3 a);
vec3 rayPoint(Ray ray, float t);
float intersectSphere(Ray ray, Sphere sphere);
vec3 sphereNormal(vec3 point, Sphere sphere);
float intersectPlane(Ray ray, Plane plane);
Hit closestHit(Ray ray);
vec3 lambertianDiffuse(vec3 N, vec3 L, vec3 diffuse_color, vec3 light_color);
vec3 phongSpecular(vec3 N, vec3 L, vec3 V, vec3 specular_color, vec3 light_color, float shininess);
vec3 blinnPhongSpecular(vec3 N, vec3 L, vec3 V, vec3 specular_color, vec3 light_color, float shininess);
vec3 castRay(Ray ray);
vec3 shade(Ray ray, vec3 hit_point, vec3 normal, vec3 obj_color);

float length2(vec3 a)
{
    return dot(a, a);
}

vec3 rayPoint(Ray ray, float t)
{
    return ray.origin + ray.direction * t;
}

float intersectSphere(Ray ray, Sphere sphere)
{
    vec3 c_o = ray.origin - sphere.center;
    float b = dot(ray.direction, c_o);
    float c = length2(c_o) - sphere.radius * sphere.radius;
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

vec3 lambertianDiffuse(vec3 N, vec3 L, vec3 diffuse_color, vec3 light_color)
{
    return max(0.0, dot(N, L)) * diffuse_color * light_color;
}

vec3 phongSpecular(vec3 N, vec3 L, vec3 V, vec3 specular_color, vec3 light_color, float shininess)
{
    float energy_conserve = 1.0;
    // energy_conserve = (2.0 + shininess) / (2.0 * PI);
    vec3 R = reflect(-L, N);
    vec3 result = energy_conserve * pow(max(0.0, dot(R, V)), shininess) * specular_color * light_color;
    result *= max(0.0, dot(L, N));
    return result;
}

vec3 blinnPhongSpecular(vec3 N, vec3 L, vec3 V, vec3 specular_color, vec3 light_color, float shininess)
{
    float energy_conserve = 1.0;
    // energy_conserve = (8.0 + shininess) / (8.0 * PI);
    vec3 H = normalize(L + V);
    vec3 result = energy_conserve * pow(max(0.0, dot(H, N)), shininess * 4.0) * max(0.0, dot(L, N)) * specular_color * light_color;
    result *= max(0.0, dot(L, N));
    return result;
}

vec3 shade(Ray ray, vec3 hit_point, vec3 normal, vec3 obj_color)
{
    // vec3 hit_point = rayPoint(ray, depth);
    vec3 result = vec3(0);

    // ambient
    result = obj_color * ambient_color * ambient_intensity;

    vec3 to_light = normalize(pl.position - hit_point);

    bool inside_shadow = false;

    Hit check_occlusion = closestHit(Ray(hit_point, to_light));

    if (check_occlusion.obj_id != -1 && check_occlusion.dist < distance(pl.position, hit_point))
    {
        inside_shadow = true;
    }

    if (!inside_shadow)
    {
        // diffuse
        result += lambertianDiffuse(normal, to_light, obj_color, pl.color);

        // specular
        vec3 specular_color = vec3(1.0, 1.0, 1.0);
        if (specular_shader_type == 0)
        {
            result += phongSpecular(to_light, normal, -ray.direction, specular_color, pl.color, 16.0);
        }
        else if (specular_shader_type == 1)
        {
            result += blinnPhongSpecular(to_light, normal, -ray.direction, specular_color, pl.color, 16.0);
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
            vec3 obj_color;
            float reflectivity;
            switch (obj_list[closest_hit.obj_id].type)
            {
                case SPHERE:
                    normal = sphereNormal(hit_point, sphere_list[obj_list[closest_hit.obj_id].index]);
                    obj_color = sphere_list[obj_list[closest_hit.obj_id].index].color;
                    reflectivity = sphere_list[obj_list[closest_hit.obj_id].index].reflectivity;
                    break;
                case PLANE:
                    normal = plane_list[obj_list[closest_hit.obj_id].index].normal;
                    obj_color = plane_list[obj_list[closest_hit.obj_id].index].color;
                    reflectivity = plane_list[obj_list[closest_hit.obj_id].index].reflectivity;
                    break;
            }

            vec3 part_result = shade(curr_ray, hit_point, normal, obj_color) * reflect_mult;

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

    //Create Ray struct from origin through current pixel
    Ray ray = Ray(cam.position, rayDir);

    FragColor = vec4(castRay(ray), 0);
}