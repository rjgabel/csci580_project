#version 330 core
out vec4 FragColor;

// PIs
#ifndef PI
#define PI 3.141592653589f
#endif

#ifndef TWO_PI
#define TWO_PI (2.0f * PI)
#endif

#ifndef FOUR_PI
#define FOUR_PI (4.0f * PI)
#endif

#ifndef EIGHT_PI
#define EIGHT_PI (8.0f * PI)
#endif

#define EPSILON 1e-5

#define LIGHT 0
#define SPHERE 1
#define PLANE 2

// Specular Type Codes
#define PHONG 0
#define BLINN_PHONG 1
#define COOK_TORRANCE 2

// Diffuse Type Codes
#define LAMBERTIAN 0
#define OREN_NAYAR 1
#define DISNEY_DIFFUSE 2

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
    float sheen_strength;
    float sheen_tint;
};

struct Object
{
    int type;
    int index;
};

struct PointLight
{
    vec3 position;
    float radius;
    vec3 color;
    float power;
};

struct DirectionLight
{
    vec3 direction;
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

float ambient_intensity = 0.2;

const Material material_list[] = Material[]
(
    Material(vec3(0.5, 0.5, 0.5), 0.0, 0.6, 0.1, 0.0, 0.0),

    Material(vec3(1.0, 0.5, 0.5), 0.0, 0.5, 0.5, 0.0, 0.0),
    Material(vec3(1.0, 0.5, 0.5), 0.1, 0.5, 0.5, 0.0, 0.0),
    Material(vec3(1.0, 0.5, 0.5), 0.2, 0.5, 0.5, 0.0, 0.0),
    Material(vec3(1.0, 0.5, 0.5), 0.3, 0.5, 0.5, 0.0, 0.0),
    Material(vec3(1.0, 0.5, 0.5), 0.4, 0.5, 0.5, 0.0, 0.0),
    Material(vec3(1.0, 0.5, 0.5), 0.5, 0.5, 0.5, 0.0, 0.0),
    Material(vec3(1.0, 0.5, 0.5), 0.6, 0.5, 0.5, 0.0, 0.0),
    Material(vec3(1.0, 0.5, 0.5), 0.7, 0.5, 0.5, 0.0, 0.0),
    Material(vec3(1.0, 0.5, 0.5), 0.8, 0.5, 0.5, 0.0, 0.0),
    Material(vec3(1.0, 0.5, 0.5), 0.9, 0.5, 0.5, 0.0, 0.0),
    Material(vec3(1.0, 0.5, 0.5), 1.0, 0.5, 0.5, 0.0, 0.0),

    Material(vec3(0.5, 1.0, 0.5), 0.5, 0.0, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 1.0, 0.5), 0.5, 0.1, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 1.0, 0.5), 0.5, 0.2, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 1.0, 0.5), 0.5, 0.3, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 1.0, 0.5), 0.5, 0.4, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 1.0, 0.5), 0.5, 0.5, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 1.0, 0.5), 0.5, 0.6, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 1.0, 0.5), 0.5, 0.7, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 1.0, 0.5), 0.5, 0.8, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 1.0, 0.5), 0.5, 0.9, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 1.0, 0.5), 0.5, 1.0, 0.5, 0.0, 0.0),

    Material(vec3(0.5, 0.5, 0.01), 0.0, 0.0, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 0.5, 0.01), 0.0, 0.1, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 0.5, 0.01), 0.0, 0.2, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 0.5, 0.01), 0.0, 0.3, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 0.5, 0.01), 0.0, 0.4, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 0.5, 0.01), 0.0, 0.5, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 0.5, 0.01), 0.0, 0.6, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 0.5, 0.01), 0.0, 0.7, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 0.5, 0.01), 0.0, 0.8, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 0.5, 0.01), 0.0, 0.9, 0.5, 0.0, 0.0),
    Material(vec3(0.5, 0.5, 0.01), 0.0, 1.0, 0.5, 0.0, 0.0),

    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 0.0, 0.0),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 0.1, 0.0),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 0.2, 0.0),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 0.3, 0.0),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 0.4, 0.0),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 0.5, 0.0),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 0.6, 0.0),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 0.7, 0.0),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 0.8, 0.0),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 0.9, 0.0),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 1.0, 0.0),

    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 1.0, 0.0),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 1.0, 0.1),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 1.0, 0.2),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 1.0, 0.3),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 1.0, 0.4),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 1.0, 0.5),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 1.0, 0.6),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 1.0, 0.7),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 1.0, 0.8),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 1.0, 0.9),
    Material(vec3(0.01, 0.01, 0.5), 0.0, 1.0, 0.7, 1.0, 1.0)
);

const Object obj_list[] = Object[]
(
    // Object(PLANE,  0),

    Object(SPHERE,  0),
    Object(SPHERE,  1),
    Object(SPHERE,  2),
    Object(SPHERE,  3),
    Object(SPHERE,  4),
    Object(SPHERE,  5),
    Object(SPHERE,  6),
    Object(SPHERE,  7),
    Object(SPHERE,  8),
    Object(SPHERE,  9),
    Object(SPHERE, 10),

    Object(SPHERE, 11),
    Object(SPHERE, 12),
    Object(SPHERE, 13),
    Object(SPHERE, 14),
    Object(SPHERE, 15),
    Object(SPHERE, 16),
    Object(SPHERE, 17),
    Object(SPHERE, 18),
    Object(SPHERE, 19),
    Object(SPHERE, 20),
    Object(SPHERE, 21),

    Object(SPHERE, 22),
    Object(SPHERE, 23),
    Object(SPHERE, 24),
    Object(SPHERE, 25),
    Object(SPHERE, 26),
    Object(SPHERE, 27),
    Object(SPHERE, 28),
    Object(SPHERE, 29),
    Object(SPHERE, 30),
    Object(SPHERE, 31),
    Object(SPHERE, 32),

    Object(SPHERE, 33),
    Object(SPHERE, 34),
    Object(SPHERE, 35),
    Object(SPHERE, 36),
    Object(SPHERE, 37),
    Object(SPHERE, 38),
    Object(SPHERE, 39),
    Object(SPHERE, 40),
    Object(SPHERE, 41),
    Object(SPHERE, 42),
    Object(SPHERE, 43),

    Object(SPHERE, 44),
    Object(SPHERE, 45),
    Object(SPHERE, 46),
    Object(SPHERE, 47),
    Object(SPHERE, 48),
    Object(SPHERE, 49),
    Object(SPHERE, 50),
    Object(SPHERE, 51),
    Object(SPHERE, 52),
    Object(SPHERE, 53),
    Object(SPHERE, 54)
);

const int p_light_num = 0;
const PointLight p_light_list[] = PointLight[]
(
    PointLight(vec3(0.0, 0.0, 0.0), 0.25, vec3(1.0, 1.0, 1.0), 200.0)
);

const int d_light_num = 1;
const DirectionLight d_light_list[] = DirectionLight[]
(
    DirectionLight(vec3(0.0, 0.0, -1.0), vec3(1.0, 1.0, 1.0), 2.5)
);

const Sphere sphere_list[] = Sphere[]
(
    Sphere(vec3(-15.0, 6.0, -30.0), 1.0, material_list[1]),
    Sphere(vec3(-12.0, 6.0, -30.0), 1.0, material_list[2]),
    Sphere(vec3( -9.0, 6.0, -30.0), 1.0, material_list[3]),
    Sphere(vec3( -6.0, 6.0, -30.0), 1.0, material_list[4]),
    Sphere(vec3( -3.0, 6.0, -30.0), 1.0, material_list[5]),
    Sphere(vec3(  0.0, 6.0, -30.0), 1.0, material_list[6]),
    Sphere(vec3(  3.0, 6.0, -30.0), 1.0, material_list[7]),
    Sphere(vec3(  6.0, 6.0, -30.0), 1.0, material_list[8]),
    Sphere(vec3(  9.0, 6.0, -30.0), 1.0, material_list[9]),
    Sphere(vec3( 12.0, 6.0, -30.0), 1.0, material_list[10]),
    Sphere(vec3( 15.0, 6.0, -30.0), 1.0, material_list[11]),

    Sphere(vec3(-15.0, 3.0, -30.0), 1.0, material_list[12]),
    Sphere(vec3(-12.0, 3.0, -30.0), 1.0, material_list[13]),
    Sphere(vec3( -9.0, 3.0, -30.0), 1.0, material_list[14]),
    Sphere(vec3( -6.0, 3.0, -30.0), 1.0, material_list[15]),
    Sphere(vec3( -3.0, 3.0, -30.0), 1.0, material_list[16]),
    Sphere(vec3(  0.0, 3.0, -30.0), 1.0, material_list[17]),
    Sphere(vec3(  3.0, 3.0, -30.0), 1.0, material_list[18]),
    Sphere(vec3(  6.0, 3.0, -30.0), 1.0, material_list[19]),
    Sphere(vec3(  9.0, 3.0, -30.0), 1.0, material_list[20]),
    Sphere(vec3( 12.0, 3.0, -30.0), 1.0, material_list[21]),
    Sphere(vec3( 15.0, 3.0, -30.0), 1.0, material_list[22]),

    Sphere(vec3(-15.0, 0.0, -30.0), 1.0, material_list[23]),
    Sphere(vec3(-12.0, 0.0, -30.0), 1.0, material_list[24]),
    Sphere(vec3( -9.0, 0.0, -30.0), 1.0, material_list[25]),
    Sphere(vec3( -6.0, 0.0, -30.0), 1.0, material_list[26]),
    Sphere(vec3( -3.0, 0.0, -30.0), 1.0, material_list[27]),
    Sphere(vec3(  0.0, 0.0, -30.0), 1.0, material_list[28]),
    Sphere(vec3(  3.0, 0.0, -30.0), 1.0, material_list[29]),
    Sphere(vec3(  6.0, 0.0, -30.0), 1.0, material_list[30]),
    Sphere(vec3(  9.0, 0.0, -30.0), 1.0, material_list[31]),
    Sphere(vec3( 12.0, 0.0, -30.0), 1.0, material_list[32]),
    Sphere(vec3( 15.0, 0.0, -30.0), 1.0, material_list[33]),

    Sphere(vec3(-15.0, -3.0, -30.0), 1.0, material_list[34]),
    Sphere(vec3(-12.0, -3.0, -30.0), 1.0, material_list[35]),
    Sphere(vec3( -9.0, -3.0, -30.0), 1.0, material_list[36]),
    Sphere(vec3( -6.0, -3.0, -30.0), 1.0, material_list[37]),
    Sphere(vec3( -3.0, -3.0, -30.0), 1.0, material_list[38]),
    Sphere(vec3(  0.0, -3.0, -30.0), 1.0, material_list[39]),
    Sphere(vec3(  3.0, -3.0, -30.0), 1.0, material_list[40]),
    Sphere(vec3(  6.0, -3.0, -30.0), 1.0, material_list[41]),
    Sphere(vec3(  9.0, -3.0, -30.0), 1.0, material_list[42]),
    Sphere(vec3( 12.0, -3.0, -30.0), 1.0, material_list[43]),
    Sphere(vec3( 15.0, -3.0, -30.0), 1.0, material_list[44]),

    Sphere(vec3(-15.0, -6.0, -30.0), 1.0, material_list[45]),
    Sphere(vec3(-12.0, -6.0, -30.0), 1.0, material_list[46]),
    Sphere(vec3( -9.0, -6.0, -30.0), 1.0, material_list[47]),
    Sphere(vec3( -6.0, -6.0, -30.0), 1.0, material_list[48]),
    Sphere(vec3( -3.0, -6.0, -30.0), 1.0, material_list[49]),
    Sphere(vec3(  0.0, -6.0, -30.0), 1.0, material_list[50]),
    Sphere(vec3(  3.0, -6.0, -30.0), 1.0, material_list[51]),
    Sphere(vec3(  6.0, -6.0, -30.0), 1.0, material_list[52]),
    Sphere(vec3(  9.0, -6.0, -30.0), 1.0, material_list[53]),
    Sphere(vec3( 12.0, -6.0, -30.0), 1.0, material_list[54]),
    Sphere(vec3( 15.0, -6.0, -30.0), 1.0, material_list[55])
);

const Plane plane_list[] = Plane[]
(
    Plane(vec3(0.0, 0.0, 1.0), vec3(0.0, 0.0, -35.0), material_list[0])
);

vec3 rayPoint(Ray ray, float t)
{
    return ray.origin + ray.direction * t;
}

float intersectLight(Ray ray, PointLight light)
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

float luminance(vec3 rgb)
{
    return dot(rgb, vec3(0.2126, 0.7152, 0.0722));
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
                check_dist = check_light ? intersectLight(ray, p_light_list[obj_list[i].index]) : check_dist;
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

float orenNayar(vec3 N, vec3 L, vec3 V, float sigma)
{
    float A = 1.0 - 0.5 * sigma / (sigma + 0.33);
    float B = 0.45 * sigma / (sigma + 0.09);

    vec3 proj_V = normalize(V - dot(V, N) * N);
    vec3 proj_L = normalize(L - dot(V, N) * N);
    float cos_phi_diff = max(0.0, dot(proj_V, proj_L));

    float theta_l = acos(dot(N, L));
    float theta_v = acos(dot(N, V));
    float alpha = max(theta_l, theta_v);
    float beta = min(theta_l, theta_v);

    return (A + B * cos_phi_diff) * sin(alpha) * tan(beta);
}

float disneyDiffuse(vec3 N, vec3 L, vec3 V, float roughness)
{
    vec3 H = normalize(L + V);
    float LdotH = max(0.0, dot(L, H));
    float NdotL = max(0.0, dot(N, L));
    float NdotV = max(0.0, dot(N, V));

    float F_D90_minus_1 = 2 * roughness * LdotH * LdotH - 0.5;

    return (1.0 + F_D90_minus_1 * pow(1 - NdotL, 5.0)) * (1.0 + F_D90_minus_1 * pow(1 - NdotV, 5.0));
}

float phong(vec3 N, vec3 L, vec3 V, float shininess)
{
    float energy_conserve = (shininess + 2.0) / TWO_PI;
    vec3 R = reflect(-L, N);
    return energy_conserve * pow(max(0.0, dot(R, V)), shininess);
}

float blinnPhong(vec3 N, vec3 L, vec3 V, float shininess)
{
    float energy_conserve = ((shininess + 2.0) * (shininess + 4.0)) / (EIGHT_PI * (pow(2.0, -shininess / 2.0) + shininess));
    vec3 H = normalize(L + V);
    return energy_conserve * pow(max(0.0, dot(N, H)), shininess);
}

float cookTorrance(vec3 N, vec3 L, vec3 V, float alpha2)
{
    alpha2 = max(EPSILON, alpha2);

    vec3 H = normalize(L + V);
    float NdotH = max(EPSILON, dot(N, H));
    float NdotL = max(EPSILON, dot(N, L));
    float NdotV = max(EPSILON, dot(N, V));
    float HdotV = max(EPSILON, dot(H, V));
    
    float cos_theta2 = NdotH * NdotH;
    float cos_theta4 = cos_theta2 * cos_theta2;
    float D = exp((cos_theta2 - 1) / (cos_theta2 * alpha2)) / (PI * alpha2 * cos_theta4);
    
    float G = min(1, min(2 * NdotH * NdotV / HdotV, 2 * NdotH * NdotL / HdotV));
    
    return D * G / (PI * NdotL * NdotV);
}

vec3 calcDiffSpec(Material material, vec3 N, vec3 L, vec3 V, vec3 F0, vec3 diffuse_reflectance, vec3 light_intensity)
{
    vec3 H = normalize(V + L);
    vec3 fresnel_term = fresnelSchlick(F0, H, V);

    float alpha = material.roughness * material.roughness;
    float alpha2 = alpha * alpha;

    // specular
    vec3 specular = vec3(0.0);
    float k_spec;

    if (specular_shader_type == PHONG)
    {
        float shininess = 2.0 / clamp(alpha2, EPSILON, 1.0 - EPSILON) - 2.0;
        k_spec = phong(N, L, V, shininess);
    }
    else if (specular_shader_type == BLINN_PHONG)
    {
        float shininess = 2.0 / clamp(alpha2, EPSILON, 1.0 - EPSILON) - 2.0;
        shininess *= 4.0;
        k_spec = blinnPhong(N, L, V, shininess);
    }
    else if (specular_shader_type == COOK_TORRANCE)
    {
        k_spec = cookTorrance(N, L, V, alpha2);
    }

    specular = k_spec * fresnel_term;

    // diffuse
    vec3 diffuse = vec3(0.0);
    float k_diff = 1.0; // default: 1.0 is Lambertian

    if (diffuse_shader_type == OREN_NAYAR)
    {
        // beckmann roughness to oren-nayar roughness
        float sigma = 0.7071068 * atan(alpha);
        k_diff = orenNayar(N, L, V, sigma);
    }
    else if (diffuse_shader_type == DISNEY_DIFFUSE)
    {
        k_diff = disneyDiffuse(N, L, V, material.roughness);
    }

    diffuse = k_diff * diffuse_reflectance / PI;
    if (diffuse_shader_type != DISNEY_DIFFUSE)
    {
        diffuse *= (1 - fresnel_term);
    }
    else
    {
        float obj_luminance = luminance(material.color);
        vec3 tint = obj_luminance > 0.0 ? normalize(material.color) : vec3(1.0);
        float HdotL = dot(H, L);
        vec3 sheen = material.sheen_strength * mix(vec3(1.0), tint, material.sheen_tint) * pow(1 - HdotL, 1.0);
        diffuse += sheen;
    }

    return (diffuse + specular) * light_intensity * max(0.0, dot(N, L));
}

vec3 shade(Ray ray, Material material, vec3 hit_point, vec3 normal, out vec3 reflectivity)
{
    vec3 result = vec3(0.0);

    vec3 to_view = -ray.direction;

    vec3 min_F0 = vec3(max(0.16 * material.reflectance * material.reflectance, 0.02));
    vec3 F0 = mix(min_F0, material.color, material.metalness);
    reflectivity = F0 * F0;

    vec3 diffuse_reflectance = material.color * (1.0 - material.metalness);

    for (int i = 0; i < p_light_num; i++)
    {
        vec3 to_light = normalize(p_light_list[i].position - hit_point);
        float to_light_dist = distance(p_light_list[i].position, hit_point);
        float to_light_dist2 = max(EPSILON, to_light_dist * to_light_dist);
        vec3 light_intensity = p_light_list[i].color * p_light_list[i].power / (FOUR_PI * to_light_dist2);
        
        // ambient
        result += diffuse_reflectance * ambient_intensity * light_intensity;

        bool inside_shadow = false;

        Hit check_occlusion = closestHit(Ray(hit_point, to_light), false);

        if (check_occlusion.obj_id != -1 && check_occlusion.dist < to_light_dist)
        {
            inside_shadow = true;
        }

        if (!inside_shadow)
        {
            result += calcDiffSpec(material, normal, to_light, to_view, F0, diffuse_reflectance, light_intensity);
        }
    }

    for (int i = 0; i < d_light_num; i++)
    {
        vec3 to_light = normalize(-d_light_list[i].direction);
        vec3 light_intensity = d_light_list[i].color * d_light_list[i].power;

        result += diffuse_reflectance * ambient_intensity * light_intensity;

        bool inside_shadow = false;

        Hit check_occlusion = closestHit(Ray(hit_point, to_light), false);

        if (check_occlusion.obj_id != -1)
        {
            inside_shadow = true;
        }

        if (!inside_shadow)
        {
            result += calcDiffSpec(material, normal, to_light, to_view, F0, diffuse_reflectance, light_intensity);
        }
    }

    return result;
}

vec3 castRay(Ray ray)
{
    vec3 result = vec3(0.0);

    Ray curr_ray = ray;
    float total_dist = 0;
    vec3 reflect_mult = vec3(1.0);
    bool hit_something = false;

    for (int i = 0; i < 5; i++)
    {
        Hit closest_hit = closestHit(curr_ray, true);

        if (closest_hit.obj_id != -1 && closest_hit.dist > cam.near && (closest_hit.dist + total_dist) < cam.far)
        {
            hit_something = true;
            if (obj_list[closest_hit.obj_id].type == LIGHT)
            {
                float to_light_dist2 = clamp(closest_hit.dist * closest_hit.dist, EPSILON, p_light_list[closest_hit.obj_id].power);
                vec3 light_intensity = p_light_list[closest_hit.obj_id].color * p_light_list[closest_hit.obj_id].power / to_light_dist2;
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

            result += shade(curr_ray, material, hit_point, normal, reflectivity) * reflect_mult;

            if (luminance(reflectivity) > EPSILON)
            {
                vec3 reflect_vec = reflect(curr_ray.direction, normal);
                curr_ray = Ray(hit_point, reflect_vec);
                reflect_mult *= reflectivity;
                total_dist += closest_hit.dist;
            }
            else
            {
                break;
            }
        }
        else
        {
            break;
        }
    }

    if (!hit_something)
    {
        return vec3(0.5);
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