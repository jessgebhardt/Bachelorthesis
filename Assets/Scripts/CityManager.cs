using UnityEngine;

public class CityManager : MonoBehaviour
{
    // DATACLASS CITYBOUNDARIES
    [SerializeField] private CityBoundariesData boundaries = new CityBoundariesData();

    // DATACLASS DISTRICTGEN
    [SerializeField] private DistrictsData districtsData = new DistrictsData();

    // DATACLASS Voronoi
    [SerializeField] private VoronoiData voronoiData = new VoronoiData();

    // DATACLASS poissonDisk
    [SerializeField] private PoissonDiskData poissonDiskData = new PoissonDiskData();

    private Renderer cityRenderer;


    private void OnValidate()
    {
        InitializeCityBoundaries();
        InitializeDistrictGenerator();
    }

    // cityboundaries
    private void InitializeCityBoundaries()
    {
        if (boundaries.lineRenderer == null)
        {
            cityRenderer = GetComponent<Renderer>();
            boundaries.lineRenderer = gameObject.GetComponent<LineRenderer>();
            CityBoundaries.InitializeLineRenderer(boundaries);
        }
        boundaries.center = transform.position;
        CityBoundaries.UpdateBoundaries(boundaries);
    }

    // DistrictGen
    private void InitializeDistrictGenerator()
    {
        districtsData.counter = -1;
        DistrictGenerator.InitializeDistricts(districtsData.relationsAndIDsInitialized, districtsData.districtTypes);
    }

    public void GenerateDistricts()
    {
        DistrictGenerator.GenerateDistricts(districtsData, boundaries, poissonDiskData);
        (voronoiData.voronoiTexture, voronoiData.regions) = VoronoiDiagram.GenerateVoronoiDiagram(districtsData, voronoiData, boundaries, cityRenderer);
        ApplyTexture();
    }

    // RoadGen
    public void GenerateRoads()
    {
        //prepareBordersScript = prepareBorders.GetComponent<BorderPreparation>();
        //voronoiTexture = prepareBordersScript.GenerateRoads(voronoiTexture, cityBoundaries.outerBoundaryRadius, cityBoundaries.transform.position, segmentLength);
        //voronoiTexture.Apply();
        //ApplyTexture();
    }

    public void RemoveRoads()
    {
        RoadGenerator.RemoveOldRoadsAndRoadSystems();
    }


    // LotGen
    // BuildingGen
    public void GenerateLotsAndBuildings()
    {
        //regionLots = LotGenerator.GenerateLots(voronoiTexture, regions, districtsDictionary, roadWidth);
        //BuildingGenerator.GenerateBuildings(regionLots, districtsDictionary);
    }

    public void RemoveBuildings()
    {
        BuildingGenerator.RemoveOldBuildings();
    }



    private void ApplyTexture()
    {
        voronoiData.voronoiTexture.Apply();
        if (cityRenderer != null)
        {
            cityRenderer.sharedMaterial.mainTexture = voronoiData.voronoiTexture;
        }
    }
}
