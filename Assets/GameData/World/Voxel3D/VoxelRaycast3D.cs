using UnityEngine;

namespace Game.World.Voxel3D
{
    public static class VoxelRaycast3D
    {
        // Grid traversal also selects vines, torches and other non-colliding blocks.
        public static bool Cast(VoxelWorld3DRuntime rt,Vector3 origin,Vector3 direction,float distance,out Vector3Int cell,out Vector3Int normal,out float traveled)
        {
            cell=Vector3Int.FloorToInt(origin);normal=Vector3Int.zero;traveled=0;
            if(rt==null||direction.sqrMagnitude<.000001f)return false;direction.Normalize();
            Vector3Int step=new Vector3Int(direction.x>=0?1:-1,direction.y>=0?1:-1,direction.z>=0?1:-1);
            Vector3 delta=new Vector3(Mathf.Abs(direction.x)>.000001f?Mathf.Abs(1/direction.x):float.PositiveInfinity,Mathf.Abs(direction.y)>.000001f?Mathf.Abs(1/direction.y):float.PositiveInfinity,Mathf.Abs(direction.z)>.000001f?Mathf.Abs(1/direction.z):float.PositiveInfinity);
            Vector3 edge=new Vector3((direction.x>=0?cell.x+1-origin.x:origin.x-cell.x)*delta.x,(direction.y>=0?cell.y+1-origin.y:origin.y-cell.y)*delta.y,(direction.z>=0?cell.z+1-origin.z:origin.z-cell.z)*delta.z);
            // Avoid 0 * infinity on rays starting exactly on an axis-aligned boundary.
            if(float.IsNaN(edge.x))edge.x=float.PositiveInfinity;if(float.IsNaN(edge.y))edge.y=float.PositiveInfinity;if(float.IsNaN(edge.z))edge.z=float.PositiveInfinity;
            for(int i=0;i<512&&traveled<=distance;i++)
            {
                if(!rt.IsLoaded(cell))return false;
                if(rt.World.GetBlock(cell.x,cell.y,cell.z)!=0)return true;
                if(edge.x<edge.y&&edge.x<edge.z){traveled=edge.x;edge.x+=delta.x;cell.x+=step.x;normal=new Vector3Int(-step.x,0,0);}
                else if(edge.y<edge.z){traveled=edge.y;edge.y+=delta.y;cell.y+=step.y;normal=new Vector3Int(0,-step.y,0);}
                else{traveled=edge.z;edge.z+=delta.z;cell.z+=step.z;normal=new Vector3Int(0,0,-step.z);}
            }
            return false;
        }
        public static bool Clear(VoxelWorld3DRuntime rt,Vector3 a,Vector3 b)
        {
            Vector3 d=b-a;float length=d.magnitude;
            return !UnityEngine.Physics.Raycast(a,d/Mathf.Max(.0001f,length),length,~(1<<2),QueryTriggerInteraction.Ignore);
        }
    }
}
