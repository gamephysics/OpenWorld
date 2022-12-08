using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

using UnityEngine;
using System;


namespace DOTS.WORLD
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class VisualUISystem : SystemBase
	{
        public int CurrSection = 0;
        protected override void OnUpdate()
		{

            //==================================================================================================
            // Camera MOVE
            //==================================================================================================

            if (Camera.main)
			{ 
				if (Input.GetKey(KeyCode.UpArrow))
				{

					Camera.main.transform.position += new Vector3(0, 0, 0.1f);

                }
				if (Input.GetKey(KeyCode.DownArrow))
				{
                    Camera.main.transform.position += new Vector3(0, 0, -0.1f);



                }
				if (Input.GetKey(KeyCode.RightArrow))
				{
                    Camera.main.transform.position += new Vector3(0.1f, 0, 0);



                }
				if (Input.GetKey(KeyCode.LeftArrow))
				{
                    Camera.main.transform.position += new Vector3(-0.1f, 0, 0);



                }
            }

            //==================================================================================================
            // SUB SCENE SECTION Load / Unload
            //==================================================================================================
            if (Camera.main)
			{
				var pos = Camera.main.transform.position;

				int x = math.clamp((int)(pos.x / 10.0f), 0, 2);
                int z = math.clamp((int)(pos.z / 10.0f), 0, 2);

				int index = z * 3 + x + 1;

                if(CurrSection != index)
                {
                    var sceneSctorSystem = World.GetExistingSystem<SceneSectionLoaderSystem>();
                    if (sceneSctorSystem != null)
                    {
                        if(sceneSctorSystem.LoadSceneSection(index))
                        {
                            sceneSctorSystem.UnloadSceneSection(CurrSection);
                            CurrSection = index;
                        }
                    }
                }
                
            }
		}
	}
}