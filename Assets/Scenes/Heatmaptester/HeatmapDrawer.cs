using UnityEngine;

public class HeatmapDrawer : MonoBehaviour
{

    public Vector4[] positions;
    public float[] radiuses;
    public float[] intensities;
    public Material material;

    void Start()
    {
        material.SetInt("_Points_Length", positions.Length);

        material.SetVectorArray("_Points", positions);

        Vector4[] properties = new Vector4[positions.Length];

        for (int i = 0; i < positions.Length; i++)
        {
            properties[i] = new Vector2(radiuses[i], intensities[i]);
        }

        material.SetVectorArray("_Properties", properties);
       // Latitude: 7.2228450625569 Longitude: 100.015796799554 //topleft
       // Latitude: 7.2228450625569 Longitude: 100.940263866297 //topright
       // Latitude: 6.835143723842 Longitude: 100.015796799554//downLeft
       // Latitude: 6.835143723842 Longitude: 100.940263866297//downright
    }
}