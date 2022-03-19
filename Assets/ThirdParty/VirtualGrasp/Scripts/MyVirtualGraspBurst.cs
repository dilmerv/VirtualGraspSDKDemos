// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

//#define ENABLE_BURST

using VirtualGrasp;
using UnityEngine;
using System;
#if ENABLE_BURST
using Unity.Jobs;
using Unity.Burst;
#endif

/**
 * MyVirtualGraspBurst is a customizable main tutorial component. 
 *
 * MyVirtualGraspBurst inherits from VG_MainScript, which wraps the main communication functions of the API.
 * VG_MainScript inherits from Monobehavior so you can use this as a component to a GameObject in Unity.
 * In contrast to MyVirtualGrasp, this component uses Burst Jobs to isolate VG updates on a seperate thread.
 */

[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_component_myvirtualgraspburst." + VG_Version.__VG_VERSION__ + ".html")]
public class MyVirtualGraspBurst : VG_MainScript
{
	[Serializable]
	public class BurstParameters
	{
		[Tooltip("0 = No Burst; 1 = single worker thread; X = X workers threads.")]
		public uint m_numThreads = 4;

	}
	public BurstParameters m_burstParameters;

#if ENABLE_BURST
	private SingleWorkerThread m_singleWorkerThread;         // Running multi threading on a single worker thread
	private MultipleWorkerThreads m_multipleWorkerThreads;   // Running multi threading on multiple worker threads
	private JobHandle m_handle;
#endif

	override public void Awake()
	{
		base.Awake();
		VG_Controller.Initialize();
		VG_ExternalControllerManager.Initialize(this);

#if ENABLE_BURST
		if (m_burstParameters.m_numThreads == 1)
			m_singleWorkerThread = new SingleWorkerThread();
		else if (m_burstParameters.m_numThreads > 1)
			m_multipleWorkerThreads = new MultipleWorkerThreads();
#endif
	}

	override public void Update()
	{
		base.Update();
	}

#if !ENABLE_BURST
	override public void FixedUpdate()
	{
		base.FixedUpdate();
	}

#else
	override public void FixedUpdate()
	{
		if (m_burstParameters.m_numThreads > 0)
		{
			//following: Use Schedule and Complete at the right time, from: https://docs.unity3d.com/Manual/JobSystemTroubleshooting.html)
			VG_Controller.IsolatedUpdateDataIn();
			m_handle = (m_burstParameters.m_numThreads == 1) ?
				m_singleWorkerThread.Schedule() : m_multipleWorkerThreads.ScheduleParallel((int)m_burstParameters.m_numThreads, 16, default);
			m_handle.Complete();
			VG_Controller.IsolatedUpdateDataOut();
		}
		else base.FixedUpdate();
	}

	[BurstCompile]
	public struct SingleWorkerThread : IJob
	{
		public void Execute()
		{
			VG_Controller.IsolatedUpdate();
		}
	}

	[BurstCompile]
	public struct MultipleWorkerThreads : IJobFor
	{
		public void Execute(int i)
		{
			VG_Controller.IsolatedUpdate();
		}
	}
#endif
}
