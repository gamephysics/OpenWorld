using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Scenes;

using UnityEngine;
using System;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.Rendering.CoreUtils;

namespace DOTS.WORLD
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]

	public partial class SceneSectionLoaderSystem : SystemBase
	{
		private SceneSystem							    m_SceneSystem;
		private Unity.Entities.Hash128				    m_SceneGuid;
		private Entity								    m_SceneEntity		= Entity.Null;
        private NativeParallelMultiHashMap<int, Entity>	m_Sections;

        protected override void OnCreate()
		{
			m_SceneSystem	= World.GetExistingSystem<SceneSystem>();
			m_SceneGuid		= m_SceneSystem.GetSceneGUID("Assets\\Scenes\\SampleScene\\SubScene.unity");
		}
        

        protected override void OnDestroy()
		{
			if(m_Sections.IsCreated) m_Sections.Dispose();
        }

		protected override void OnUpdate()
		{
            // Scene 의 SubScene Section Entity 들의 정보를 모아온다.
            CollectedSceneSections();
		}


		private void CollectedSceneSections()
		{
            // SubScene Section List 를 획득했는지
            if (m_Sections.IsCreated)
				return;
			// Scene 로딩을 통해 SubScene 이 기동되어졌는지
			if (m_SceneEntity == Entity.Null)
				m_SceneEntity = m_SceneSystem.GetSceneEntity(m_SceneGuid);
			if (m_SceneEntity == Entity.Null)
				return;

            // SubScene 에서 Section 을 획득가능한 상황인지..
            if (EntityManager.HasComponent<ResolvedSectionEntity>(m_SceneEntity) == false)
				return;

            // SubScene Section List Collect !!
            var sectionBuffer = EntityManager.GetBuffer<ResolvedSectionEntity>(m_SceneEntity);
			if (sectionBuffer.IsCreated == false)
				return;

            if (sectionBuffer.Length > 0)
			{
                m_Sections = new NativeParallelMultiHashMap<int, Entity>(sectionBuffer.Length, Allocator.Persistent);

				for(int i = 0; i < sectionBuffer.Length; ++i)
				{
					if(EntityManager.HasComponent<SceneSectionData>(sectionBuffer[i].SectionEntity))
                    {
						var SectionData = EntityManager.GetComponentData<SceneSectionData>(sectionBuffer[i].SectionEntity);
                        m_Sections.Add(SectionData.SubSectionIndex, sectionBuffer[i].SectionEntity);
                    }
                }
			}
            //========================================================
            // SubScene 이 Loading 되면 Section ID 0 은 기본 Load 되어지도록 한다.
            //========================================================
            // https://docs.unity3d.com/Packages/com.unity.entities@0.51/manual/loading_scenes.html
            // Section 0 is the default section.
            LoadSceneSection(0);
        }

        public bool IsLoadedAllSceneSection(int Section)
        {
            //========================================================
            // 작동금지
            //========================================================
            if (m_Sections.IsCreated == false)
                return false;

            if (m_Sections.CountValuesForKey(Section) <= 0)
                return false;

            //========================================================
            // Loaded Check
            //========================================================
            for (bool Success = m_Sections.TryGetFirstValue(Section, out var sectionEntity, out var iter); 
                      Success; 
                      Success = m_Sections.TryGetNextValue(out sectionEntity, ref iter))
            { 
                if (m_SceneSystem.IsSectionLoaded(sectionEntity) == true)
                    continue;

                if (EntityManager.HasComponent<RequestSceneLoaded>(sectionEntity) == true)
                    continue;

                return false;

            }
            return true;
		}

		public bool LoadSceneSection(int Section)
		{
            //========================================================
            // 작동금지
            //========================================================
            if (m_Sections.IsCreated == false)
                return false;

            if (m_Sections.CountValuesForKey(Section) <= 0)
                return false;

            //========================================================
            // Loaded Check
            //========================================================
            for (bool Success = m_Sections.TryGetFirstValue(Section, out var sectionEntity, out var iter); 
                      Success; 
                      Success = m_Sections.TryGetNextValue(out sectionEntity, ref iter))
            {
                if (m_SceneSystem.IsSectionLoaded(sectionEntity) == true)
                    continue;

                if (EntityManager.HasComponent<RequestSceneLoaded>(sectionEntity) == true)
                    continue;

                //========================================================
                // Load Request
                //========================================================
                EntityManager.AddComponent<RequestSceneLoaded>(sectionEntity);
            }

            Debug.Log($"Section Load Request {Section}");

            return true;
		}

		public bool UnloadSceneSection(int Section)
		{
            //========================================================
            // 0 Section 은 Default
            //========================================================
            if (Section <= 0)
				return false;

            //========================================================
            // 작동금지
            //========================================================
            if (m_Sections.IsCreated == false)
                return false;

            if (m_Sections.CountValuesForKey(Section) <= 0)
                return false;

            //========================================================
            // UnLoaded Check
            //========================================================
            for (bool Success = m_Sections.TryGetFirstValue(Section, out var sectionEntity, out var iter); 
                      Success; 
                      Success = m_Sections.TryGetNextValue(out sectionEntity, ref iter))
			{            
				if (m_SceneSystem.IsSectionLoaded(sectionEntity) == false)
					continue;

				if(EntityManager.HasComponent<RequestSceneLoaded>(sectionEntity) == false)
                    continue;

				//========================================================
				// UnLoad Request
				//========================================================
				EntityManager.RemoveComponent<RequestSceneLoaded>(sectionEntity);
            }

            Debug.Log($"Section UnLoad Request {Section}");

            return true;
		}
    }
}

