#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture : register(t0);
Texture2D MaskTexture : register(t1);
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
    float4 maskColor = tex2D(MaskTextureSampler, input.TextureCoordinates);
	
    return maskColor;
	
    clip(maskColor.a - 0.1f);
	
    float2 texCoord = input.TextureCoordinates;
    texCoord.x = texCoord.x * Tiling.x;
    texCoord.y = texCoord.y * Tiling.y;
    texCoord.x += 1 - abs(sin(Time)) / 2;

    return tex2D(SpriteTextureSampler, texCoord) * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};