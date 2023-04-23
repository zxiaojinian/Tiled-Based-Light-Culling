#ifndef TILED_BASE_CS_INCLUDE
#define TILED_BASE_CS_INCLUDE

#define TILE_SIZE 16
#define MAX_NUM_LIGHTS_PER_TILE 32

#define SPOT_LIGHT 0
#define DIRECTIONAL_LIGHT 1
#define POINT_LIGHT 2

struct ComputeShaderInput
{
    uint3 groupID           : SV_GroupID;
    uint3 groupThreadID     : SV_GroupThreadID;
    uint3 dispatchThreadID  : SV_DispatchThreadID;
    uint  groupIndex        : SV_GroupIndex;
};

//约定：ndc z统一为[-1,1],view 统一为右手坐标系

float4x4 _InverseProjection;
float4 _ScreenParams_CS;//w,h,1/w,1/h
uint _NumTilesX;
uint _NumTilesY;
//float4 _CameraParams_CS;//near,far

float4 NDCToView(float4 clip)
{
    float4 view = mul(_InverseProjection, clip);
    view = view / view.w;
    return view;
}
 
float4 ScreenToView(float4 screen)
{
    float2 screenPos01 = screen.xy * _ScreenParams_CS.zw;
    float4 ndc = float4(screenPos01 * 2.0f - 1.0f, screen.z * 2.0 - 1.0, screen.w);
    return NDCToView(ndc);
}

//np - d = 0
struct Plane
{
    float3 N;
    float  d;
};

struct Frustum
{
    Plane planes[4];   // L,R,T,B,法线朝内
};


struct FrustumCone
{
    float3 coneDir;
    float2 coneSinCos;
};

struct Sphere
{
    float3 c;
    float  r;
};

struct LightCone
{
    float4 ConeParams;  //灯光包围圆锥数据(lightDistSqr, lightDist, lightSin ,lightCos)，圆锥顶点在相机位置
    float4 ConeDir; //xyz灯光包围圆锥方向，圆锥顶点在相机位置,z,range
};

struct LightConeSphere
{
    LightCone coneData;
    Sphere sphereData;
};

struct Cone
{
    float3 T;   // Cone tip.
    float  h;   // Height of the cone.
    float3 d;   // Direction of the cone.
    float  r;   // Bottom radius of the cone.
};


//输入为观察空间，逆时针
Plane ComputePlane(float3 p0, float3 p1, float3 p2)
{
    Plane plane;
 
    float3 v0 = p1 - p0;
    float3 v2 = p2 - p0;
    plane.N = normalize(cross(v0, v2));
    plane.d = dot(plane.N, p0);
 
    return plane;
}

//https://www.3dgep.com/forward-plus/

// Check to see if a sphere is fully behind (inside the negative halfspace of) a plane.
// Source: Real-time collision detection, Christer Ericson (2005)
//dot(c,n) - d
bool SphereInsidePlane(Sphere sphere, Plane plane)
{
    return dot(plane.N, sphere.c) - plane.d < - sphere.r;
}

// Check to see of a light is partially contained within the frustum.
bool SphereInsideFrustum(Sphere sphere, Frustum frustum, float zNear, float zFar) 
{
    bool result = true;

    // First check depth
    // Note: Here, the view vector points in the -Z axis so the 
    // far depth value will be approaching -infinity.
    if ( sphere.c.z - sphere.r > zNear || sphere.c.z + sphere.r < zFar )
    {
        result = false;
    }
 
    // Then check frustum planes
    for ( int i = 0; i < 4 && result; i++ )
    {
        if ( SphereInsidePlane( sphere, frustum.planes[i] ) )
        {
            result = false;
        }
    }

    return result;
}

// Check to see if a point is fully behind (inside the negative halfspace of) a plane.
bool PointInsidePlane(float3 p, Plane plane)
{
    return dot(plane.N, p) - plane.d < 0;
}

// Check to see if a cone if fully behind (inside the negative halfspace of) a plane.
// Source: Real-time collision detection, Christer Ericson (2005)
bool ConeInsidePlane(Cone cone, Plane plane) 
{
    // Compute the farthest point on the end of the cone to the positive space of the plane.
    float3 m = cross(cross(plane.N, cone.d), cone.d);
    float3 Q = cone.T + cone.d * cone.h - m * cone.r;

    // The cone is in the negative halfspace of the plane if both
    // the tip of the cone and the farthest point on the end of the cone to the
    // positive halfspace of the plane are both inside the negative halfspace
    // of the plane.
    return PointInsidePlane(cone.T, plane) && PointInsidePlane(Q, plane);
}


bool ConeInsideFrustum(Cone cone, Frustum frustum, float zNear, float zFar)
{
    bool result = true;

    Plane nearPlane = {float3(0, 0, -1), -zNear};//d=dot(p,n)
    Plane farPlane = {float3(0, 0, 1), zFar};

    // First check the near and far clipping planes.
    if (ConeInsidePlane(cone, nearPlane) || ConeInsidePlane(cone, farPlane))
    {
        result = false;
    }

    // Then check frustum planes
    for (int i = 0; i < 4 && result; i++)
    {
        if (ConeInsidePlane( cone, frustum.planes[i]))
        {
            result = false;
        }
    }

    return result;
}

struct AABB
{
	float3 c; // center
	float3 e; // half extents
};

AABB ComputeFrustumAABB(uint2 groupID, float minZ, float maxZ)
{
    AABB aabb = (AABB)0;

    uint px = TILE_SIZE * groupID.x;
    uint py = TILE_SIZE * groupID.y;
    uint pxp = TILE_SIZE * (groupID.x + 1);
    uint pyp = TILE_SIZE * (groupID.y + 1);

    //远裁剪面
    float3 frustumTL = ScreenToView(float4(px, pyp, 1.f, 1.f)).xyz;
    float3 frustumBR = ScreenToView(float4(pxp, py, 1.f, 1.f)).xyz;

    float factorMax = maxZ / frustumTL.z;
    float2 frustumTopLeftAtBack = frustumTL.xy * factorMax;//相似三角形
    float2 frustumBottomRightAtBack = frustumBR.xy * factorMax;
    float factorMin = minZ / frustumTL.z;
    float2 frustumTopLeftAtFront = frustumTL.xy * factorMin;
    float2 frustumBottomRightAtFront = frustumBR.xy * factorMin;

    float2 frustumMinXY = min( frustumTopLeftAtBack, min( frustumBottomRightAtBack, min(frustumTopLeftAtFront, frustumBottomRightAtFront) ) );
    float2 frustumMaxXY = max( frustumTopLeftAtBack, max( frustumBottomRightAtBack, max(frustumTopLeftAtFront, frustumBottomRightAtFront) ) );

    float3 frustumAABBMin = float3(frustumMinXY.x, frustumMinXY.y, minZ);
    float3 frustumAABBMax = float3(frustumMaxXY.x, frustumMaxXY.y, maxZ);

    aabb.c = (frustumAABBMin + frustumAABBMax) * 0.5f;
    aabb.e = abs(frustumAABBMax - aabb.c);

    return aabb;
}

void ComputeFrustumDualAABB(uint2 groupID, float minZ, float maxZ2, float minZ2, float maxZ, out AABB aabb1,out AABB aabb2)
{
    uint px = TILE_SIZE * groupID.x;
    uint py = TILE_SIZE * groupID.y;
    uint pxp = TILE_SIZE * (groupID.x + 1);
    uint pyp = TILE_SIZE * (groupID.y + 1);

    //远裁剪面
    float3 frustumTL = ScreenToView(float4(px, pyp, 1.f, 1.f)).xyz;
    float3 frustumBR = ScreenToView(float4(pxp, py, 1.f, 1.f)).xyz;

    {
        //naer aabb
        float factorMax = maxZ2 / frustumTL.z;
        float2 frustumTopLeftAtBack = frustumTL.xy * factorMax;//相似三角形
        float2 frustumBottomRightAtBack = frustumBR.xy * factorMax;
        float factorMin = minZ / frustumTL.z;
        float2 frustumTopLeftAtFront = frustumTL.xy * factorMin;
        float2 frustumBottomRightAtFront = frustumBR.xy * factorMin;

        float2 frustumMinXY = min( frustumTopLeftAtBack, min( frustumBottomRightAtBack, min(frustumTopLeftAtFront, frustumBottomRightAtFront) ) );
        float2 frustumMaxXY = max( frustumTopLeftAtBack, max( frustumBottomRightAtBack, max(frustumTopLeftAtFront, frustumBottomRightAtFront) ) );

        float3 frustumAABBMin = float3(frustumMinXY.x, frustumMinXY.y, minZ);
        float3 frustumAABBMax = float3(frustumMaxXY.x, frustumMaxXY.y, maxZ2);

        aabb1.c = (frustumAABBMin + frustumAABBMax) * 0.5f;
        aabb1.e = abs(frustumAABBMax - aabb1.c);
    }

    {
        //far aabb
        float factorMax = maxZ / frustumTL.z;
        float2 frustumTopLeftAtBack = frustumTL.xy * factorMax;//相似三角形
        float2 frustumBottomRightAtBack = frustumBR.xy * factorMax;
        float factorMin = minZ2 / frustumTL.z;;
        float2 frustumTopLeftAtFront = frustumTL.xy * factorMin;
        float2 frustumBottomRightAtFront = frustumBR.xy * factorMin;

        float2 frustumMinXY = min( frustumTopLeftAtBack, min( frustumBottomRightAtBack, min(frustumTopLeftAtFront, frustumBottomRightAtFront) ) );
        float2 frustumMaxXY = max( frustumTopLeftAtBack, max( frustumBottomRightAtBack, max(frustumTopLeftAtFront, frustumBottomRightAtFront) ) );

        float3 frustumAABBMin = float3(frustumMinXY.x, frustumMinXY.y, minZ2);
        float3 frustumAABBMax = float3(frustumMaxXY.x, frustumMaxXY.y, maxZ);

        aabb2.c = (frustumAABBMin + frustumAABBMax) * 0.5f;
        aabb2.e = abs(frustumAABBMax - aabb2.c);
    }
}

bool SphereIntersectsAABB(Sphere sphere, AABB aabb)
{
	float3 vDelta = max(0, abs(aabb.c - sphere.c) - aabb.e);
	float fDistSq = dot(vDelta, vDelta);
	return fDistSq <= sphere.r * sphere.r;
}

//https://lxjk.github.io/2018/03/25/Improve-Tile-based-Light-Culling-with-Spherical-sliced-Cone.html
bool SphericalSlicedCone(LightCone lightConeData, float3 tileCenterVec, float2 tileConeSinCos, float tileMinDis, float tileMaxDis)
{
    float lightDistSqr = lightConeData.ConeParams.x;
    float lightDist = lightConeData.ConeParams.y;
    float3 lightCenterVec = lightConeData.ConeDir.xyz;
    float lightSin = lightConeData.ConeParams.z;
    float lightCos = lightConeData.ConeParams.w;
    float lightRadius = lightConeData.ConeDir.w;

    float lightTileCos = dot(lightCenterVec, tileCenterVec);
    float lightTileSin = sqrt(1 - lightTileCos * lightTileCos);
    // sum angle = light cone half angle + tile cone half angle
    float sumCos = (lightRadius > lightDist) ? -1.0 : (tileConeSinCos.y * lightCos - tileConeSinCos.x * lightSin);

    // diff angle = sum angle - tile cone half angle
    // clamp to handle the case when light center is within tile cone
    float diffSin = clamp(lightTileSin * tileConeSinCos.y - lightTileCos * tileConeSinCos.x, 0.0, 1.0);
    float diffCos = (diffSin == 0.0) ? 1.0 : lightTileCos * tileConeSinCos.y + lightTileSin * tileConeSinCos.x;
    float lightTileDistOffset = sqrt(lightRadius * lightRadius - lightDistSqr * diffSin * diffSin);
    float lightTileDistBase = lightDist * diffCos;

    float lightMin = lightTileDistBase - lightTileDistOffset;
    float lightMax = lightTileDistBase + lightTileDistOffset;
    return lightTileCos >= sumCos && lightMin <= tileMaxDis && lightMax >= tileMinDis;
}

bool SphericalSlicedCone_2(LightCone lightConeData, float3 tileCenterVec, float2 tileConeSinCos, float tileMinDis, float tileMaxDis, out float2 lightMinMax)
{
    float lightDistSqr = lightConeData.ConeParams.x;
    float lightDist = lightConeData.ConeParams.y;
    float3 lightCenterVec = lightConeData.ConeDir.xyz;
    float lightSin = lightConeData.ConeParams.z;
    float lightCos = lightConeData.ConeParams.w;
    float lightRadius = lightConeData.ConeDir.w;

    float lightTileCos = dot(lightCenterVec, tileCenterVec);
    float lightTileSin = sqrt(1 - lightTileCos * lightTileCos);
    // sum angle = light cone half angle + tile cone half angle
    float sumCos = (lightRadius > lightDist) ? -1.0 : (tileConeSinCos.y * lightCos - tileConeSinCos.x * lightSin);

    // diff angle = sum angle - tile cone half angle
    // clamp to handle the case when light center is within tile cone
    float diffSin = clamp(lightTileSin * tileConeSinCos.y - lightTileCos * tileConeSinCos.x, 0.0, 1.0);
    float diffCos = (diffSin == 0.0) ? 1.0 : lightTileCos * tileConeSinCos.y + lightTileSin * tileConeSinCos.x;
    float lightTileDistOffset = sqrt(lightRadius * lightRadius - lightDistSqr * diffSin * diffSin);
    float lightTileDistBase = lightDist * diffCos;

    float lightMin = lightTileDistBase - lightTileDistOffset;
    float lightMax = lightTileDistBase + lightTileDistOffset;
    lightMinMax.x = lightMin;
    lightMinMax.y = lightMax;
    return lightTileCos >= sumCos && lightMin <= tileMaxDis && lightMax >= tileMinDis;
}

bool _2_5_D_Test(float lightMin, float lightMax, float tileMin, float tileRangeRecip, uint tileMask)
{
    uint lightMaskStart = max(0, min(31, floor((lightMin - tileMin) * tileRangeRecip)));
    uint lightMaskEnd = max(0, min(31, floor((lightMax - tileMin) * tileRangeRecip)));
    uint lightMask = 0xFFFFFFFF;
    lightMask >>= 31 - (lightMaskEnd - lightMaskStart);
    lightMask <<= lightMaskStart;
    return tileMask & lightMask;
}
#endif