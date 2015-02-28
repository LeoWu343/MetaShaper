using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Meta;
using MeshHelper;
using System;

public class MainMesh : MonoBehaviour
{
    public enum Mode { MOVE, ADD };
    public Mode mode = Mode.MOVE;
    static Vector3[] newPoints;
    public Hashtable vertexToObject = new Hashtable();
    public GameObject handlePreFab;
    Hashtable controlPoints = new Hashtable();
    public HashSet<int> selectedIds = new HashSet<int>();
    public Stack<Vector3[]> undoStack = new Stack<Vector3[]>();
    public Stack<Vector3[]> redoStack = new Stack<Vector3[]>();

    private VertexHandler handler;
    // Use this for initialization
    void Start()
    {
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        MeshHelper.MeshHelper.Subdivide (mesh);
        MeshHelper.MeshHelper.Subdivide (mesh);

        Vector3[] parentVertices = mesh.vertices;
        for (int i = 0; i < parentVertices.Length; i++)
        {
            Vector3 parentWorldPosition = gameObject.transform.TransformPoint(parentVertices[i]);
            if (!vertexToObject.ContainsKey(parentWorldPosition))
            {
                GameObject child = Instantiate(handlePreFab) as GameObject;
                child.GetComponent<VertexHandler>().setId(i);
                child.GetComponent<MetaBody>().grabbable = false;
                vertexToObject[parentWorldPosition] = child;
                controlPoints[i] = child;
                child.transform.parent = gameObject.transform;
                child.transform.position = parentWorldPosition;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

        Vector3 leftHandCenter = Hands.left.pointer.position;
        Vector3 rightHandCenter = Hands.right.pointer.position;
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] parentVertices = mesh.vertices;
        for (int i = 0; i < parentVertices.Length; i++)
        {
            Vector3 vertexWorldPosition = gameObject.transform.TransformPoint(parentVertices[i]);
            GameObject vertex = (GameObject)(controlPoints[i]);
            if ((Hands.right.gesture.type == MetaGesture.GRAB) && (Hands.left.gesture.type == MetaGesture.GRAB))
            {
                if (vertex != null)
                {
                    vertex.GetComponent<MetaBody>().grabbable = false;
                }
            }
            else
            {
                if (vertex != null && selectedIds.Contains(i))
                {
                    if (mode == Mode.MOVE)
                    {
                        vertex.GetComponent<MetaBody>().grabbable = true;
                    }
                }
            }
        }
    }

    public void PushUndo()
    {
        Mesh meshSnapshot = gameObject.GetComponent<MeshFilter>().mesh;
        undoStack.Push(meshSnapshot.vertices);
        redoStack.Clear();
    }

    public void Undo()
    {
        if (undoStack.Count > 0)
        {
            Vector3[] snapshot = undoStack.Pop();
            Mesh nesh = gameObject.GetComponent<MeshFilter>().mesh;
            redoStack.Push(nesh.vertices);
            nesh.vertices = snapshot;
        }
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] parentVertices = mesh.vertices;
        foreach (DictionaryEntry entry in controlPoints)
        {
            ((GameObject)entry.Value).transform.position = gameObject.transform.TransformPoint(parentVertices[(int)entry.Key]);
        }
    }

    public void Redo()
    {
        if (redoStack.Count > 0) {
            Vector3[] snapshot = redoStack.Pop();
            Mesh rnesh = gameObject.GetComponent<MeshFilter>().mesh;
            undoStack.Push(rnesh.vertices);
            rnesh.vertices = snapshot;
        }
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] parentVertices = mesh.vertices;
        foreach (DictionaryEntry entry in controlPoints)
        {
            ((GameObject)entry.Value).transform.position = gameObject.transform.TransformPoint(parentVertices[(int)entry.Key]);
        }
    }

    public void MoveMode(Boolean toggle)
    {

        if (mode == Mode.MOVE)
        {
            mode = Mode.ADD;
        }
        else
        {
            mode = Mode.MOVE;
        }
        removeAllSelected();
        Debug.Log("Mode " + mode.ToString());
    }
    public void AddMode(Boolean toggle)
    {

        /*mode = Mode.ADD;
        removeAllSelected();
        Debug.Log("Add Mode " + toggle);*/
    }

    public void handleSelect(int vertexId, bool selected)
    {
        if (selected)
        {
            if (mode == Mode.MOVE)
            {
                ((GameObject)controlPoints[vertexId]).GetComponent<MetaBody>().grabbable = true;
                ((GameObject)controlPoints[vertexId]).GetComponent<MetaBody>().pinchable = true;
            }
            ((GameObject)controlPoints[vertexId]).GetComponent<Renderer>().material.color = Color.cyan;
            selectedIds.Add(vertexId);
        }
        else
        {
            if (mode == Mode.MOVE)
            {
                ((GameObject)controlPoints[vertexId]).GetComponent<MetaBody>().grabbable = false;
                ((GameObject)controlPoints[vertexId]).GetComponent<MetaBody>().pinchable = false;
            }
            ((GameObject)controlPoints[vertexId]).GetComponent<Renderer>().material.color = Color.red;
            selectedIds.Remove(vertexId);
        }
        if ((selectedIds.Count == 3) && (mode == Mode.ADD))
        {
            AddVertex();
        }
    }

    public void AddVertex()
    {
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] parentVertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        List<int> indices = new List<int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            bool foundFirst = false;
            bool foundSecond = false;
            bool foundThird = false;

            foreach (int vertexId in selectedIds)
            {
                if (gameObject.transform.TransformPoint(parentVertices[triangles[i + 0]]) == ((GameObject)controlPoints[vertexId]).transform.position)
                {
                    foundFirst = true;
                }
            }
            foreach (int vertexId in selectedIds)
            {
                if (gameObject.transform.TransformPoint(parentVertices[triangles[i + 1]]) == ((GameObject)controlPoints[vertexId]).transform.position)
                {
                    foundSecond = true;
                }
            }       
            foreach (int vertexId in selectedIds)
            {
                if (gameObject.transform.TransformPoint(parentVertices[triangles[i + 2]]) == ((GameObject)controlPoints[vertexId]).transform.position)
                {
                    foundThird = true;
                }
            }
            if (foundFirst && foundSecond && foundThird)
            {
                
                int ia = triangles[i + 0];
                int ib = triangles[i + 1];
                int ic = triangles[i + 2];

                Vector3 pa = gameObject.transform.TransformPoint(parentVertices[triangles[i + 0]]);
                Vector3 pb = gameObject.transform.TransformPoint(parentVertices[triangles[i + 1]]);
                Vector3 pc = gameObject.transform.TransformPoint(parentVertices[triangles[i + 2]]);
                Vector3 pabc_world_pos = (pa+pb+pc)/3.0f;
                Vector3 pabc = gameObject.transform.InverseTransformPoint(pabc_world_pos);

                List<Vector3> new_vertices = new List<Vector3>(mesh.vertices);
                new_vertices.Add(pabc);
                int iabc = new_vertices.Count - 1;

                indices.Add(ia);
                indices.Add(ib);
                indices.Add(iabc);
                indices.Add(ib);
                indices.Add(ic);
                indices.Add(iabc);
                indices.Add(ic);
                indices.Add(ia);
                indices.Add(iabc);
                mesh.vertices = new_vertices.ToArray();


                GameObject child = Instantiate(handlePreFab) as GameObject;
                child.GetComponent<VertexHandler>().setId(iabc);
                child.GetComponent<MetaBody>().grabbable = false;
                vertexToObject[pabc_world_pos] = child;
                controlPoints[iabc] = child;
                child.transform.parent = gameObject.transform;
                child.transform.position = pabc_world_pos;


            }
            else
            {
                indices.Add(triangles[i + 0]);
                indices.Add(triangles[i + 1]);
                indices.Add(triangles[i + 2]);
            }
        }
        mesh.triangles = indices.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        removeAllSelected();


    }
    public void removeAllSelected()
    {
        foreach (int vertexId in selectedIds)
        {
            ((GameObject)controlPoints[vertexId]).GetComponent<VertexHandler>().selected = false;
            ((GameObject)controlPoints[vertexId]).GetComponent<MetaBody>().grabbable = false;
            ((GameObject)controlPoints[vertexId]).GetComponent<MetaBody>().pinchable = false;
            ((GameObject)controlPoints[vertexId]).GetComponent<Renderer>().material.color = Color.red;
        }
        selectedIds.Clear();
    }

    public void MovedControlPoint(int i, Vector3 delta, Vector3 startingPoint, Vector3 endingPoint)
    {
        //WarpMesh(i, startingPoint, endingPoint, 0.1f, 10.0f, 0.1f);
        //Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        //DeformMesh(mesh, endingPoint, 10*delta.sqrMagnitude, 0.5f);

        MeshWarp(i, endingPoint);
        /*for (int j = 0; j < vertices.Length; j++)
        {
            Vector3 vertDelt = vertices[j] - newVertices[j];
            MovedControlPointExact(j, vertDelt, newVertices[j]);
        }*/
        //Debug.Log("Moved control point.");
    }

    public void MovedControlPointExact(int id, Vector3 delta, Vector3 finalPosition)
    {
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] parentVertices = mesh.vertices;
        Vector3 meshPointPosition = gameObject.transform.TransformPoint(parentVertices[id]);
        MoveMeshPoint(meshPointPosition, finalPosition);
        foreach (int i in selectedIds)
        {
            meshPointPosition = gameObject.transform.TransformPoint(parentVertices[i]);
            MoveMeshPoint(meshPointPosition, meshPointPosition + delta);
        }
    }

    public void snapControlPoints()
    {
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] parentVertices = mesh.vertices;
        foreach (int index in selectedIds)
        {
            ((GameObject)controlPoints[index]).transform.position = gameObject.transform.TransformPoint(parentVertices[index]);
        }
    }

    public void MoveMeshPoint(Vector3 meshPoint, Vector3 endPoint)
    {
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] parentVertices = mesh.vertices;
        for (int i = 0; i < parentVertices.Length; i++)
        {
            float threshold = .0000001f;
            float distanceSquared = (meshPoint - gameObject.transform.TransformPoint(parentVertices[i])).sqrMagnitude;
            if (distanceSquared <= threshold)
            {
                parentVertices[i] = gameObject.transform.InverseTransformPoint(endPoint);
            }
        }
        mesh.vertices = parentVertices;
        foreach (DictionaryEntry entry in controlPoints)
        {
            ((GameObject)entry.Value).transform.position = gameObject.transform.TransformPoint(parentVertices[(int)entry.Key]);
        }


    }

    public void MeshWarp(int index, Vector3 pos)
    {
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        pos = gameObject.transform.InverseTransformPoint(pos);
        Vector3 delta = pos - vertices[index];
        for (int i = 0; i < vertices.Length; ++i)
        {
            float distance = (vertices[i] - vertices[index]).sqrMagnitude;
            float scale = (float)(1.0 / Math.Pow(20.0 + (50.0 * distance), (2.5 / 2.0)));
            MoveMeshPoint(gameObject.transform.TransformPoint(vertices[i]), gameObject.transform.TransformPoint(vertices[i] + (scale * delta)));
        }

    }
    /*
    float radius = 1.0f;
    float pull = 10.0f;
    private MeshFilter unappliedMesh;

    static float LinearFalloff (float distance, float inRadius) {
	    return Mathf.Clamp01(1.0f - distance / inRadius);
    }

    static float GaussFalloff (float distance, float inRadius) {
	    return Mathf.Clamp01(Mathf.Pow (360.0f, -Mathf.Pow (distance / inRadius, 2.5f) - 0.01f));
    }

    void DeformMesh (Mesh mesh, Vector3 position, float power, float inRadius)
    {
	    Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
	    float sqrRadius = inRadius * inRadius;
	
	    // Calculate averaged normal of all surrounding vertices	
	    var averageNormal = Vector3.zero;
	    for (var i=0;i<vertices.Length;i++)
	    {
		    var sqrMagnitude = (vertices[i] - position).sqrMagnitude;
		    // Early out if too far away
		    if (sqrMagnitude > sqrRadius)
			    continue;

		    var distance = Mathf.Sqrt(sqrMagnitude);
		    var falloff = LinearFalloff(distance, inRadius);
		    averageNormal += falloff * normals[i];
	    }
	    averageNormal = averageNormal.normalized;
	
	    // Deform vertices along averaged normal
	    for (int i=0;i<vertices.Length;i++)
	    {
		    float sqrMagnitude = (vertices[i] - position).sqrMagnitude;
		    // Early out if too far away
		    if (sqrMagnitude > sqrRadius)
			    continue;

		    float distance = Mathf.Sqrt(sqrMagnitude);
			float falloff = GaussFalloff(distance, inRadius);

		
		    vertices[i] += averageNormal * falloff * power;
	    }
	
	    mesh.vertices = vertices;
	    mesh.RecalculateNormals();
	    mesh.RecalculateBounds();
    }
    */


    /* public void WarpMesh(int index, Vector3 start_drag, Vector3 end_drag, float gain, float falloff, float dist_offset) {
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        float select_x = vertices[index].x;
        float select_y = vertices[index].y;
        float select_z = vertices[index].z;

        float dx = end_drag.x - start_drag.x;
        float dy = end_drag.y - start_drag.y;
        float dz = end_drag.z - start_drag.z;

        for (int i = 0; i < vertices.Length; ++i)
        {
            float distance = (float)Math.Pow((vertices[i] - vertices[index]).sqrMagnitude, falloff / 2.0);
            vertices[i].x = vertices[i].x + gain * dx / (distance + dist_offset);
            vertices[i].y = vertices[i].y + gain * dy / (distance + dist_offset);
            vertices[i].z = vertices[i].z + gain * dz / (distance + dist_offset);
        }
        mesh.vertices = vertices;
    } */
}
