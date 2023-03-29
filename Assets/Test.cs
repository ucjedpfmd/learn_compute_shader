using System;
using UnityEngine;

namespace DefaultNamespace {
    public struct ParticleData {
        public Vector3 pos;
        public Color color;
    }
    
    public class Test : MonoBehaviour {

        public ComputeShader computeShader;
        public Material material;
        // public ComputeBuffer mParticleDataBuffer = new ComputeBuffer();
        public RenderTexture mRenderTexture;

        public ComputeBuffer buffer;

        public int kernelID;
        public int mParticleCount = 20000;
        private void Start() {


            buffer = new ComputeBuffer(mParticleCount, 28);
            var particleDatas = new ParticleData[mParticleCount];
            buffer.SetData(particleDatas);
            kernelID = computeShader.FindKernel("UpdateParticle");

            // mRenderTexture = new RenderTexture(2048, 2048, 16);
            // mRenderTexture.enableRandomWrite = true;
            // mRenderTexture.Create();
            // material.mainTexture = mRenderTexture;
            // var kernelIndex = computeShader.FindKernel("CSMain");
            // computeShader.SetTexture(kernelIndex,"Result",mRenderTexture);
            // computeShader.Dispatch(kernelIndex,2048/8,2048/8,1);
        }

        private void Update() {
            computeShader.SetBuffer(kernelID,"ParticleBuffer",buffer);
            computeShader.SetFloat("Time",Time.time);
            computeShader.Dispatch(kernelID,mParticleCount / 1000 , 1,1);
            material.SetBuffer("_particleDataBuffer",buffer);
        }

        private void OnRenderObject() {
            material.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points,mParticleCount);
        }

        private void OnDestroy() {
            buffer.Release();
            buffer = null;
        }
    }
}