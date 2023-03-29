using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestViewPortCulling : MonoBehaviour {
    public int instanceCount;
    public Material instanceMaterial;
    public Mesh instanceMesh;
    public int subMeshIndex;
    

    private ComputeBuffer argsBuffer;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    public ComputeShader computeShader;
    private ComputeBuffer localToWorldMatrixBuffer;
    private ComputeBuffer cullResult;
    int cachedInstanceCount = -1;
    int cachedSubMeshIndex = -1;
    
    private int kernel;

    private Camera mainCamera;
    // Start is called before the first frame update
    void Start() {
        kernel = computeShader.FindKernel("ViewPortCulling");
        mainCamera = Camera.main;
        cullResult = new ComputeBuffer(instanceCount, sizeof(float) * 16, ComputeBufferType.Append);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint),ComputeBufferType.IndirectArguments);
    }

    // Update is called once per frame
    void Update() {
        if(cachedInstanceCount != instanceCount || cachedSubMeshIndex != subMeshIndex)
            UpdateBuffers();
    
        var planes = GetFrustumPlane(mainCamera);
        
        computeShader.SetBuffer(kernel,"input",localToWorldMatrixBuffer);
        cullResult.SetCounterValue(0);
        computeShader.SetBuffer(kernel,"cullresult",cullResult);
        computeShader.SetInt("instanceCount",instanceCount);
        computeShader.SetVectorArray("planes",planes);
        
        computeShader.Dispatch(kernel,1 + (instanceCount / 640),1,1);
        instanceMaterial.SetBuffer("positionBuffer",cullResult);
        
        ComputeBuffer.CopyCount(cullResult,argsBuffer,sizeof(uint));
        
        Graphics.DrawMeshInstancedIndirect(instanceMesh,subMeshIndex,instanceMaterial,new Bounds(Vector3.zero, new Vector3(200f,200f,200f)),argsBuffer);
    }
    
    void UpdateBuffers() {
        // Ensure submesh index is in range
        if(instanceMesh != null)
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);

        if(localToWorldMatrixBuffer != null)
            localToWorldMatrixBuffer.Release();

        localToWorldMatrixBuffer = new ComputeBuffer(instanceCount, 16 * sizeof(float));
        List<Matrix4x4> localToWorldMatrixs = new List<Matrix4x4>();
        for(int i = 0; i < instanceCount; i++) {
            float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
            float distance = Random.Range(20.0f, 100.0f);
            float height = Random.Range(-2.0f, 2.0f);
            float size = Random.Range(0.05f, 0.25f);
            Vector4 position = new Vector4(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance, size);
            localToWorldMatrixs.Add(Matrix4x4.TRS(position, Quaternion.identity, new Vector3(size, size, size)));
        }
        localToWorldMatrixBuffer.SetData(localToWorldMatrixs);

        // Indirect args
        if(instanceMesh != null) {
            args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
            args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
        } else {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        argsBuffer.SetData(args);

        cachedInstanceCount = instanceCount;
        cachedSubMeshIndex = subMeshIndex;
    }

    private static Vector4[] GetFrustumPlane(Camera camera) {
        var planes = new Vector4[6];
        var transform = camera.transform;
        var cameraPosition = transform.position;
        var points = GetCameraFarClipPlanePoint(camera);

        planes[0] = GetPlane(cameraPosition, points[0], points[2]);
        planes[1] = GetPlane(cameraPosition, points[3], points[1]);
        planes[2] = GetPlane(cameraPosition, points[1], points[0]);
        planes[3] = GetPlane(cameraPosition, points[2], points[3]);
        planes[4] = GetPlane(-transform.forward, transform.position + transform.forward * camera.nearClipPlane);
        planes[5] = GetPlane(transform.forward, transform.position + transform.forward * camera.farClipPlane);
        return planes;
    }

    private static Vector3[] GetCameraFarClipPlanePoint(Camera camera) {
        var points = new Vector3[4];
        var transform = camera.transform;
        var distance = camera.farClipPlane;
        var halfFovRad = Mathf.Deg2Rad * camera.fieldOfView * 0.5f;
        var upLen = distance * Mathf.Tan(halfFovRad);
        var rightLen = upLen * camera.aspect;
        var farCenterPoint = transform.position + distance * transform.forward;
        var up = upLen * transform.up;
        var right = rightLen * transform.right;
        points[0] = farCenterPoint - up - right;//left-bottom
        points[1] = farCenterPoint - up + right;//right-bottom
        points[2] = farCenterPoint + up - right;//left-up
        points[3] = farCenterPoint + up + right;//left-up
        return points;
    }

    private static Vector4 GetPlane(Vector3 a, Vector3 b, Vector3 c) {
        var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        return GetPlane(normal, a);
    }

    private static Vector4 GetPlane(Vector3 normal, Vector3 point) {
        return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, point));
    }
}
