#version 330 core

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

struct Camera
{
    vec3 position;
    float near;
    float far;
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

struct Plane
{
    vec3 normal;
    vec3 point;
    vec3 color;
};

struct Object
{
    uint type;
    uint index;
};

struct Sphere
{
    vec3 center;
    float radius;
    vec3 color;
};

struct Ray
{
    vec3 origin;
    vec3 direction;
};

Sphere sphere = Sphere(vec3(0, -1.0, -7.0), 1.0, vec3(1.0, 0.0, 0.0));

Plane plane = Plane(vec3(0.0, 1.0, 0.0), vec3(0.0, -2.0, 0.0), vec3(0.0, 0.1, 0.8));

PointLight pl = PointLight(vec3(-4.0, 1.0, 0.0), vec3(1.0, 1.0, 1.0));

vec3 ambient_color = vec3(1.0);
float ambient_intensity = 0.2;

in vec4 FragPos;

uniform mat4 inv_view_proj;
uniform Camera cam;
uniform int specular_shader_type;

float intersectSphere(Ray ray, Sphere sphere)
{
	// Sphere center to ray origin
    vec3 co = ray.origin - sphere.center;

	// The discriminant is negative for a miss, or a postive value
	// used to calcluate the distance from the ray origin to point of intersection
    //bear in mind that there may be more than one solution
    float discriminant = dot(co, ray.direction) * dot(co, ray.direction) - (dot(co, co) - sphere.radius * sphere.radius);

	// If answer is not negative, get ray intersection depth
    if (discriminant >= 0.0)
        return -dot(ray.direction, co) - sqrt(discriminant);
    else
        return -1.; // Any negative number to indicate no intersect
}

vec3 sphereNormal(vec3 point, Sphere sphere)
{
    return normalize(point - sphere.center);
}

float intersectPlane(Ray ray, Plane plane)
{
    float denominator = dot(ray.direction, plane.normal);
    if (abs(denominator) >= 0.01)
    {//make sure Ray is not parallel to plane (or nearly parallel)
        return dot((plane.point - ray.origin), plane.normal) / denominator;
    }
    return -1.0; // Any negative number to indicate no intersect (or intersection from behind)
}

//Get the direction to the light source (no shadows simplifies check)
vec3 checkLight(vec3 intersect, PointLight light)
{
    return normalize(light.position - intersect);
}

vec3 lambertianDiffuse(vec3 normal, vec3 to_light, vec3 diffuse_color, vec3 light_color)
{
    return max(0.0, dot(normal, to_light)) * diffuse_color * light_color;
}

vec3 phongSpecular(vec3 normal, vec3 to_light, vec3 to_viewer, vec3 specular_color, vec3 light_color, float shininess)
{
    float energy_conserve = 1.0;
    // energy_conserve = (2.0 + shininess) / (2.0 * PI);
    vec3 reflected = reflect(-to_light, normal);
    vec3 result = energy_conserve * pow(max(0.0, dot(reflected, to_viewer)), shininess) * specular_color * light_color;
    result *= max(0.0, dot(to_light, normal));
    return result;
}

vec3 blinnPhongSpecular(vec3 normal, vec3 to_light, vec3 to_viewer, vec3 specular_color, vec3 light_color, float shininess)
{
    float energy_conserve = 1.0;
    // energy_conserve = (8.0 + shininess) / (8.0 * PI);
    vec3 halfway = normalize(to_light + to_viewer);
    vec3 result = energy_conserve * pow(max(0.0, dot(halfway, normal)), shininess * 4.0) * max(0.0, dot(to_light, normal)) * specular_color * light_color;
    result *= max(0.0, dot(to_light, normal));
    return result;
}

vec3 shade(Ray ray, float depth, vec3 normal, PointLight pl, vec3 obj_color, bool inside_shadow)
{
    vec3 hit = ray.origin + ray.direction * depth;
    vec3 to_light = checkLight(hit, pl);

    vec3 result = vec3(0);

    // ambient
    result = obj_color * ambient_color * ambient_intensity;

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

void main()
{
    vec4 worldPos = inv_view_proj * FragPos;
    vec3 pixelPos = worldPos.xyz / worldPos.w;

    vec3 rayDir = normalize(pixelPos - cam.position);

    //Create Ray struct from origin through current pixel
    Ray ray = Ray(cam.position, rayDir);

    // Check if the ray from the camera through the pixel intersects the sphere
    float sphereDepth = intersectSphere(ray, sphere);
    float planeDepth = intersectPlane(ray, plane);

    bool sphereHit = sphereDepth > cam.near && sphereDepth < cam.far;
    bool planeHit = planeDepth > cam.near && planeDepth < cam.far;

    if (sphereHit)
    {
        if (planeDepth < sphereDepth && planeHit)
        {
            vec3 plane_normal = plane.normal;
            vec3 plane_color = shade(ray, planeDepth, plane_normal, pl, plane.color, true);

            gl_FragColor = vec4(plane_color * 0.5, 1);
        }
        else
        {
            vec3 hit_point = ray.origin + ray.direction * sphereDepth;
            vec3 sphere_normal = sphereNormal(hit_point, sphere);
            vec3 sphere_color = shade(ray, sphereDepth, sphere_normal, pl, sphere.color, false);

            gl_FragColor = vec4(sphere_color, 1);
        }
    }
    else if (planeHit)
    {

        vec3 hit_point = ray.origin + ray.direction * planeDepth;
        vec3 to_light = normalize(pl.position - hit_point);
        vec3 view_light = normalize(pl.position - cam.position);
        Ray light_ray = Ray(hit_point, to_light);
        Ray view_light_ray = Ray(cam.position, view_light);

        float sphere_check = intersectSphere(light_ray, sphere);
        float block_check = intersectPlane(view_light_ray, plane);
        float block_dist = length(view_light_ray.direction * block_check);
        float view_light_dist = length(pl.position - cam.position);
        bool inside_shadow = (sphere_check > 0.0 && sphere_check < cam.far) || (block_check > 0.0 && block_dist < view_light_dist);

        vec3 plane_normal = plane.normal;
        vec3 plane_color = shade(ray, planeDepth, plane_normal, pl, plane.color, inside_shadow);

        vec3 reflected_color = vec3(0.0, 0.0, 0.0);
        vec3 reflect_dir = reflect(ray.direction, plane_normal);
        Ray reflect_ray = Ray(hit_point, reflect_dir);
        float second_depth = intersectSphere(reflect_ray, sphere);
        if (second_depth > 0.01 && second_depth < cam.far)
        {
            vec3 second_obj_loc = reflect_ray.origin + reflect_ray.direction * second_depth;
            Ray cam_to_sphere = Ray(cam.position, normalize(second_obj_loc - cam.position));

            float r_sphere_depth = intersectSphere(cam_to_sphere, sphere);

            if (r_sphere_depth > cam.near && r_sphere_depth < cam.far)
            {
			    //we hit the sphere in the reflection
                vec3 hit_point_r = reflect_ray.origin + reflect_ray.direction * second_depth;
                vec3 sphere_normal = sphereNormal(hit_point_r, sphere);
                reflected_color = shade(reflect_ray, second_depth, sphere_normal, pl, sphere.color, false);

            }

        }

        gl_FragColor = vec4(plane_color * 0.5 + reflected_color * 0.5, 1);

    }
    else
    {
        // else draw background color (black)
        gl_FragColor = vec4(0, 0, 0, 1);
    }
}