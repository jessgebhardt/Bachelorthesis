using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CityGenerator : MonoBehaviour
{
    [SerializeField]
    private DistrictData districtData = new DistrictData();
    public DistrictData DistrictData => districtData;

    [SerializeField]
    private PoissonData poissonData = new PoissonData();
    public PoissonData PoissonData => poissonData;

    [SerializeField]
    private VoronoiData voronoiData = new VoronoiData();
    public VoronoiData VoronoiData => voronoiData;

    [SerializeField]
    private BoundariesData boundariesData = new BoundariesData();
    public BoundariesData BoundariesData => boundariesData;

    [SerializeField]
    private RoadData roadData = new RoadData();
    public RoadData RoadData => roadData;

    private static int constantDistrictTypes;

    private void OnValidate()
    {
        ValidateDistrictColors();

        if (constantDistrictTypes != districtData.districtTypes.Count)
        {
            InitializeRelationsAndIDs();
        }

        if(!districtData.initialized)
        {
            boundariesData.lineRenderer = gameObject.GetComponent<LineRenderer>();
            CityBoundaries.InitializeBoundaries(boundariesData);
            SetMaterialToTransparent();
            districtData.initialized = true;
        }
    }

    private void Update()
    {
        CityBoundaries.UpdateBoundaries(boundariesData);
    }

    private void ValidateDistrictColors()
    {
        if (districtData.districtTypes.Count > 0)
        {
            for (int i = 0; i < districtData.districtTypes.Count; i++)
            {
                DistrictType districtType = districtData.districtTypes[i];
                if (districtType.color == Color.black)
                {
                    districtType.color = AdjustBlackColor(districtType.color);
                    districtData.districtTypes[i] = districtType;
                }
            }
        }
    }

    private Color AdjustBlackColor(Color color)
    {
        return new Color(color.r + 0.1f, color.g + 0.1f, color.b + 0.1f, color.a);
    }

    private void InitializeRelationsAndIDs()
    {
        if (districtData.districtTypes.Count > 0)
        {
            for (int i = 0; i < districtData.districtTypes.Count; i++)
            {
                DistrictType districtType = districtData.districtTypes[i];
                districtType.id = i;
                districtType.relations.Clear();

                for (int j = 0; j < districtData.districtTypes.Count; j++)
                {
                    if (i == j) { continue; }

                    DistrictType relatedDistrictType = districtData.districtTypes[j];
                    districtType.relations.Add(new DistrictRelation
                    {
                        districtTypeId = relatedDistrictType.id,
                        _name = relatedDistrictType.name,
                        attraction = 0,
                        repulsion = 0
                    });
                }
                districtData.districtTypes[i] = districtType;
            }
            constantDistrictTypes = districtData.districtTypes.Count;
        }
    }

    public void GenerateDistricts()
    {
        boundariesData.center = transform.position;

        CalculateMinAndMaxDistricts();
        GenerateCandidatePositions();

        DistrictSelector.SelectDistrictPositions(poissonData, districtData, boundariesData);

        voronoiData.voronoiTexture = VoronoiDiagram.GenerateVoronoiDiagram(districtData, voronoiData, boundariesData, roadData);
        ApplyTexture();
    }


    public void GenerateSecondaryRoads()
    {
        voronoiData.voronoiTexture = SecondaryRoadsGenerator.GenerateSecondaryRoads(voronoiData.voronoiTexture, roadData);
        ApplyTexture();
    }


    public void GenerateLotsAndBuildings()
    {
        districtData.regionLots = LotGenerator.GenerateLots(voronoiData.voronoiTexture, voronoiData.regions, districtData.districtsDictionary, roadData.roadWidth);
        BuildingGenerator.GenerateBuildings(districtData.regionLots, districtData.districtsDictionary);
    }

    public void RemoveBuildings()
    {
        BuildingGenerator.RemoveOldBuildings();
    }

    private void CalculateMinAndMaxDistricts()
    {
        int minNumberOfDistricts = 0;
        int maxNumberOfDistricts = 0;
        foreach (DistrictType districtType in districtData.districtTypes)
        {
            minNumberOfDistricts += districtType.minNumberOfPlacements;
            maxNumberOfDistricts += districtType.maxNumberOfPlacements;
        }
        districtData.minNumberOfDistricts = minNumberOfDistricts;
        districtData.maxNumberOfDistricts = maxNumberOfDistricts;
    }

    private void GenerateCandidatePositions()
    {
        List<Vector3> allPoints = PoissonDiskSampling.GenerateDistrictPoints(districtData.numberOfDistricts, districtData.minNumberOfDistricts, districtData.maxNumberOfDistricts, boundariesData.outerBoundaryRadius, boundariesData.center, poissonData.rejectionSamples);
        districtData.numberOfDistricts = PoissonDiskSampling.ValidateNumberOfDistricts(districtData.numberOfDistricts, districtData.minNumberOfDistricts, districtData.maxNumberOfDistricts, false);
        poissonData.candidatePoints = allPoints;
    }

    private void ApplyTexture()
    {
        voronoiData.voronoiTexture.Apply();
        Renderer renderer = voronoiData.voronoiDiagram.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial.mainTexture = voronoiData.voronoiTexture;
        }
    }

    private void SetMaterialToTransparent()
    {
        Material material = voronoiData.voronoiDiagram.GetComponent<MeshRenderer>().sharedMaterial;
        if (material != null)
        {
            material.SetFloat("_Mode", 3);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}