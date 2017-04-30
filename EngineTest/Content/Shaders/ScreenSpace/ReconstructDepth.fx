﻿
//Depth Reconstruction from linear depth buffer, TheKosmonaut 2016

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  VARIABLES
////////////////////////////////////////////////////////////////////////////////////////////////////////////

float4x4 Projection;

float3 FrustumCorners[4]; //In Viewspace!

float FarClip;

Texture2D DepthMap;

SamplerState texSampler
{
    Texture = (DepthMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = POINT;
    MinFilter = POINT;
    Mipfilter = POINT;
};
 
////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  STRUCTS
////////////////////////////////////////////////////////////////////////////////////////////////////////////

struct VertexShaderInput
{
    float2 Position : POSITION0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  FUNCTIONS
////////////////////////////////////////////////////////////////////////////////////////////////////////////

	////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//  VERTEX SHADER
	////////////////////////////////////////////////////////////////////////////////////////////////////////////

VertexShaderOutput VertexShaderFunction(VertexShaderInput input, uint id:SV_VERTEXID)
{
	VertexShaderOutput output;
	output.Position = float4(input.Position, 0, 1);
	output.TexCoord.x = (float)(id / 2) * 2.0;
	output.TexCoord.y = 1.0 - (float)(id % 2) * 2.0;

	return output;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//  PIXEL SHADER
	////////////////////////////////////////////////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//  HELPER FUNCTIONS
		////////////////////////////////////////////////////////////////////////////////////////////////////////////

float TransformDepth(float depth, matrix trafoMatrix)
{
	return (depth*trafoMatrix._33 + trafoMatrix._43) / (depth * trafoMatrix._34 + trafoMatrix._44);
}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//  Main function
		////////////////////////////////////////////////////////////////////////////////////////////////////////////

float PixelShaderFunction(VertexShaderOutput input) : DEPTH
{
	float2 texCoord = float2(input.TexCoord);

	float linearDepth = DepthMap.Sample(texSampler, texCoord).r * -FarClip;

	return TransformDepth(linearDepth, Projection);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//  TECHNIQUES
	////////////////////////////////////////////////////////////////////////////////////////////////////////////

technique RestoreDepth
{
	pass Pass1
	{
		VertexShader = compile vs_5_0 VertexShaderFunction();
		PixelShader = compile ps_5_0 PixelShaderFunction();
	}
}

