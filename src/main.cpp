#include <iostream>
#include <string>
#include <sstream>

#include <glad/gl.h>
#include <GLFW/glfw3.h>
#include <stb_image.h>

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

#include <shader.hpp>
#include <camera.hpp>

using namespace std;
using namespace glm;

void framebuffer_size_callback(GLFWwindow *window, int width, int height);
void processInput(GLFWwindow *window);
void mouse_callback(GLFWwindow *window, double xpos, double ypos);
void scroll_callback(GLFWwindow *window, double xoffset, double yoffset);

// settings
const unsigned int SCR_WIDTH = 800;
const unsigned int SCR_HEIGHT = 600;

Camera camera(vec3(0.0, 0.0, 3.0));

bool firstMouse = true;
float lastX = SCR_WIDTH / 2.0;
float lastY = SCR_HEIGHT / 2.0;

float deltaTime = 0.0;
float lastFrame = 0.0;

// Key : Code = Type

// 0 : 0 = phong
// 1 : 1 = blinn-phong
// 2 : 2 = cook-torrance
int specular_shader_type = 0;

// L : 0 = lambertian
// O : 1 = oren-nayar
// K : 2 = disney
int diffuse_shader_type = 0;

int main(int argc, char **argv)
{
    if (argc > 2)
    {
        cerr << "Too many parameters" << endl;
        return -1;
    }

    int demo_num;

    if (argc < 2)
    {
        demo_num = 1;
    }

    if (argc == 2)
    {
        istringstream arg_stream(argv[1]);

        if (!(arg_stream >> noskipws >> demo_num))
        {
            cerr << "Invalid parameter input type" << endl;
            return -1;
        }

        if (demo_num < 1 || demo_num > 2)
        {
            cerr << "Invalid demo number" << endl;
            return -1;
        }
    }

    if (demo_num == 1)
    {
        camera.LimitMin = vec3(-10.0, -2.0, -15.0);
        camera.LimitMax = vec3(10.0, 50.0, 15.0);
    }

    glfwInit();
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3);
    glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);

    GLFWwindow *window = glfwCreateWindow(SCR_WIDTH, SCR_HEIGHT, "BRDF Zoo", NULL, NULL);
    if (window == NULL)
    {
        cout << "Failed to create GLFW window" << endl;
        glfwTerminate();
        return -1;
    }
    glfwMakeContextCurrent(window);

    int version = gladLoadGL(glfwGetProcAddress);
    if (version == 0)
    {
        printf("Failed to initialize OpenGL context\n");
        return -1;
    }

    // Successfully loaded OpenGL
    cout << "Loaded OpenGL " << GLAD_VERSION_MAJOR(version) << "." << GLAD_VERSION_MINOR(version) << endl;

    glfwSetFramebufferSizeCallback(window, framebuffer_size_callback);
    glfwSetInputMode(window, GLFW_CURSOR, GLFW_CURSOR_DISABLED);
    glfwSetCursorPosCallback(window, mouse_callback);
    glfwSetScrollCallback(window, scroll_callback);

    // tell stb_image.h to flip loaded texture's on the y-axis (before loading model).
    stbi_set_flip_vertically_on_load(true);

    glEnable(GL_DEPTH_TEST);

    Shader *lightingShader;

    string scene_frag_shader = "scene";

    scene_frag_shader += to_string(demo_num) + ".fs";
    cout << "Selected demo " << demo_num << endl;

    try
    {
        lightingShader = new Shader("scene_vert.vs", scene_frag_shader.c_str());
    }
    catch (const int e)
    {
        glfwTerminate();
        return e;
    }

    // camera.LimitMin = vec3(-10.0, -2.0, -15.0);
    // camera.LimitMax = vec3(10.0, 50.0, 15.0);

    float vertices[] = {
        -1.0f, -1.0f, 0.0f, // bottom left
         1.0f, -1.0f, 0.0f, // bottom right
        -1.0f,  1.0f, 0.0f, // top left
         1.0f,  1.0f, 0.0f  // top right
    };

    unsigned int VAO, VBO;
    glGenVertexArrays(1, &VAO);
    glBindVertexArray(VAO);

    glGenBuffers(1, &VBO);
    glBindBuffer(GL_ARRAY_BUFFER, VBO);
    glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);

    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 3 * sizeof(float), (void *)0);
    glEnableVertexAttribArray(0);

    lightingShader->use();

    // render loop
    // -----------
    while (!glfwWindowShouldClose(window))
    {
        // per-frame time logic
        // --------------------
        float currentFrame = static_cast<float>(glfwGetTime());
        deltaTime = currentFrame - lastFrame;
        lastFrame = currentFrame;

        // render
        // ------
        glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

        lightingShader->use();
        mat4 projection = perspective(radians(camera.Zoom), (float)SCR_WIDTH / (float)SCR_HEIGHT, 0.1f, 1000.0f);
        mat4 inv_view_proj = inverse(projection * camera.GetViewMatrix());

        lightingShader->setMat4("inv_view_proj", inv_view_proj);
        lightingShader->setVec3("cam.position", camera.Position);
        // lightingShader->setVec3("cam.forward", camera.Front);
        // lightingShader->setVec3("cam.up", camera.Up);
        // lightingShader->setVec3("cam.right", camera.Right);
        lightingShader->setFloat("cam.near", 0.1);
        lightingShader->setFloat("cam.far", 1000.0);
        lightingShader->setInt("specular_shader_type", specular_shader_type);
        lightingShader->setInt("diffuse_shader_type", diffuse_shader_type);
        glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);

        // input
        // -----
        processInput(window);

        // glfw: swap buffers and poll IO events (keys pressed/released, mouse moved etc.)
        // -------------------------------------------------------------------------------
        glfwSwapBuffers(window);
        glfwPollEvents();
    }

    delete lightingShader;

    // optional: de-allocate all resources once they've outlived their purpose:
    // ------------------------------------------------------------------------
    glDeleteVertexArrays(1, &VAO);
    glDeleteBuffers(1, &VBO);

    glfwTerminate();
    return 0;
}

void framebuffer_size_callback(GLFWwindow *window, int width, int height)
{
    glViewport(0, 0, width, height);
}

void processInput(GLFWwindow *window)
{
    if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS)
    {
        glfwSetInputMode(window, GLFW_CURSOR, GLFW_CURSOR_NORMAL);
    }
    if (glfwGetMouseButton(window, GLFW_MOUSE_BUTTON_LEFT) == GLFW_PRESS)
    {
        glfwSetInputMode(window, GLFW_CURSOR, GLFW_CURSOR_DISABLED);
    }

    if (glfwGetInputMode(window, GLFW_CURSOR) == GLFW_CURSOR_NORMAL)
    {
        return;
    }

    if (glfwGetKey(window, GLFW_KEY_W) == GLFW_PRESS)
    {
        camera.ProcessKeyboard(FORWARD, deltaTime);
    }
    if (glfwGetKey(window, GLFW_KEY_S) == GLFW_PRESS)
    {
        camera.ProcessKeyboard(BACKWARD, deltaTime);
    }
    if (glfwGetKey(window, GLFW_KEY_A) == GLFW_PRESS)
    {
        camera.ProcessKeyboard(LEFT, deltaTime);
    }
    if (glfwGetKey(window, GLFW_KEY_D) == GLFW_PRESS)
    {
        camera.ProcessKeyboard(RIGHT, deltaTime);
    }
    if (glfwGetKey(window, GLFW_KEY_SPACE) == GLFW_PRESS)
    {
        camera.ProcessKeyboard(UP, deltaTime);
    }
    if (glfwGetKey(window, GLFW_KEY_LEFT_SHIFT) == GLFW_PRESS)
    {
        camera.ProcessKeyboard(DOWN, deltaTime);
    }

    if (glfwGetKey(window, GLFW_KEY_0) == GLFW_PRESS)
    {
        specular_shader_type = 0;
    }
    if (glfwGetKey(window, GLFW_KEY_1) == GLFW_PRESS)
    {
        specular_shader_type = 1;
    }
    if (glfwGetKey(window, GLFW_KEY_2) == GLFW_PRESS)
    {
        specular_shader_type = 2;
    }

    if (glfwGetKey(window, GLFW_KEY_L) == GLFW_PRESS)
    {
        diffuse_shader_type = 0;
    }
    if (glfwGetKey(window, GLFW_KEY_O) == GLFW_PRESS)
    {
        diffuse_shader_type = 1;
    }
    if (glfwGetKey(window, GLFW_KEY_K) == GLFW_PRESS)
    {
        diffuse_shader_type = 2;
    }
}

void mouse_callback(GLFWwindow *window, double xpos, double ypos)
{
    if (glfwGetInputMode(window, GLFW_CURSOR) == GLFW_CURSOR_NORMAL)
    {
        firstMouse = true;
        return;
    }

    if (firstMouse)
    {
        lastX = xpos;
        lastY = ypos;
        firstMouse = false;
    }

    float xoffset = xpos - lastX;
    float yoffset = lastY - ypos;
    lastX = xpos;
    lastY = ypos;

    camera.ProcessMouseMovement(xoffset, yoffset);
}

void scroll_callback(GLFWwindow *window, double xoffset, double yoffset)
{
    if (glfwGetInputMode(window, GLFW_CURSOR) == GLFW_CURSOR_NORMAL)
    {
        return;
    }

    camera.ProcessMouseScroll(static_cast<float>(yoffset));
}