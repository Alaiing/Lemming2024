#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D MaskTexture : register(t1);
uniform float AlphaClip;
uniform float Time;
uniform float2 Tiling;

sampler2D SpriteTextureSampler : register(s0) = sampler_state
{
    Texture = (SpriteTexture);
};

sampler2D MaskTextureSampler : register(s1) = sampler_state
{
    Texture = (MaskTexture);
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 maskTexCoord = input.TextureCoordinates;
    float4 maskColor = tex2D(MaskTextureSampler, input.TextureCoordinates);
	
    clip(maskColor.a - 0.1f);
    
    clip(AlphaClip - maskColor.a);
    
    clip(maskColor.g + maskColor.b - 0.1f);
       
    float2 texCoord = input.TextureCoordinates;
    texCoord.x = texCoord.x * Tiling.x;
    texCoord.y = texCoord.y * Tiling.y;
    
    float gMask = step(maskColor.g, 0);
    float bMask = step(maskColor.b, 0);
    
    texCoord.x += gMask * abs(sin(Time)) / 2 + bMask * (1 - abs(sin(Time)) / 2);
    texCoord.x = gMask * (1 - texCoord.x) + bMask * texCoord.x;
    
    return tex2D(SpriteTextureSampler, texCoord) * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};