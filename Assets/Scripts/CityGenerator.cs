using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CityGenerator : MonoBehaviour
{
    #region Serialized Fields
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
    #endregion


    #region Private Fields
    private static int constantDistrictTypes;
    #endregion


    #region Initialization and Validation Methods

    /// <summary>
    /// Called when the script is loaded or a value is changed in the inspector.
    /// <para>
    /// Validates district colors, initializes relations and IDs if the district types count changes.
    /// Also initializes boundaries if not already done.
    /// </para>
    /// </summary>
    private void OnValidate()
    {
        if (constantDistrictTypes != districtData.districtTypes.Count)
        {
            InitializeRelationsAndIDs();
        }

        if (!districtData.initialized)
        {
            InitializeBoundaries();
            SetMaterialToTransparent();
            districtData.initialized = true;
        }

        ValidateDistrictColors();
    }

    /// <summary>
    /// Called once per frame.
    /// <para>
    /// Updates the city boundaries.
    /// </para>
    /// </summary>
    private void Update()
    {
        if (boundariesData.center != gameObject.transform.position)
        {
            boundariesData.center = gameObject.transform.position;
        }
        CityBoundaries.UpdateBoundaries(boundariesData);
    }

    /// <summary>
    /// Sets the material of the Voronoi diagram to be transparent, so that transparent pixels
    /// appear transparent.
    /// </summary>
    private void SetMaterialToTransparent()
    {
        var material = voronoiData.voronoiDiagram.GetComponent<MeshRenderer>().sharedMaterial;
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

    /// <summary>
    /// Ensures that none of the district types have the color black assigned.
    /// <para>
    /// Adjusts any district type color that is black to a slightly lighter shade of black.
    /// </para>
    /// </summary>
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

    /// <summary>
    /// Adjusts the color slightly.
    /// <para>
    /// Increases the red, green, and blue components of the color.
    /// </para>
    /// </summary>
    /// <param name="color">The original color to adjust.</param>
    /// <returns>The adjusted color.</returns>
    private Color AdjustBlackColor(Color color)
    {
        return new Color(color.r + 0.1f, color.g + 0.1f, color.b + 0.1f, color.a);
    }

    /// <summary>
    /// Initializes the relations and IDs for all district types.
    /// <para>
    /// Sets up relationships between district types and assigns IDs based on their index.
    /// </para>
    /// </summary>
    private void InitializeRelationsAndIDs()
    {
        for (int i = 0; i < districtData.districtTypes.Count; i++)
        {
            var districtType = districtData.districtTypes[i];
            districtType.id = i;
            districtType.relations.Clear();

            for (int j = 0; j < districtData.districtTypes.Count; j++)
            {
                if (i == j) continue;

                var relatedDistrictType = districtData.districtTypes[j];
                districtType.relations.Add(new DistrictRelation
                {
                    districtTypeId = relatedDistrictType.id,
                    _name = relatedDistrictType.name,
                    attraction = 0,
                    repulsion = 0
                });
            }
        }

        constantDistrictTypes = districtData.districtTypes.Count;
    }

    /// <summary>
    /// Initializes the LineRenderer and the boundaries for the city.
    /// <para>
    /// Sets up the LineRenderer component and initializes city boundaries.
    /// </para>
    /// </summary>
    private void InitializeBoundaries()
    {
        boundariesData.lineRenderer = gameObject.GetComponent<LineRenderer>();
        CityBoundaries.InitializeBoundaries(boundariesData);
    }
    #endregion


    #region Generation Methods

    /// <summary>
    /// Generates the city's districts based on the provided data.
    /// <para>
    /// Calculates the minimum and maximum number of districts, generates candidate positions for the districts, 
    /// selects district positions from the candidate positions, and lastly generates a Voronoi diagram to represent the districts and primary roads.
    /// </para>
    /// </summary>
    public void GenerateDistrictsAndPrimaryRoads()
    {
        boundariesData.center = gameObject.transform.position;
        CalculateMinAndMaxDistricts();
        GenerateCandidatePositions();

        DistrictSelector.SelectDistrictPositions(poissonData, districtData, boundariesData);

        voronoiData.voronoiTexture = VoronoiDiagram.GenerateVoronoiDiagram(districtData, voronoiData, boundariesData, roadData);
        ApplyTexture();
    }

    /// <summary>
    /// Generates secondary roads for the city.
    /// <para>
    /// Updates the Voronoi texture with secondary roads and applies the updated texture.
    /// </para>
    /// </summary>
    public void GenerateSecondaryRoads()
    {
        voronoiData.voronoiTexture = SecondaryRoadsGenerator.GenerateSecondaryRoads(voronoiData.voronoiTexture, roadData);
        ApplyTexture();
    }

    /// <summary>
    /// Generates lots and buildings based on the current city layout.
    /// <para>
    /// Generates lots for each district and creates buildings within those lots.
    /// </para>
    /// </summary>
    public void GenerateLotsAndBuildings()
    {
        districtData.regionLots = LotGenerator.GenerateLots(voronoiData.voronoiTexture, voronoiData.regions, districtData.districtsDictionary, roadData.roadWidth);
        BuildingGenerator.GenerateBuildings(districtData.regionLots, districtData.districtsDictionary);
    }

    /// <summary>
    /// Removes all buildings from the city.
    /// </summary>
    public void RemoveBuildings()
    {
        BuildingGenerator.RemoveOldBuildings();
    }
    #endregion


    #region Helper Methods

    /// <summary>
    /// Calculates the minimum and maximum number of districts based on the district type data.
    /// </summary>
    private void CalculateMinAndMaxDistricts()
    {
        int minNumberOfDistricts = 0;
        int maxNumberOfDistricts = 0;

        foreach (var districtType in districtData.districtTypes)
        {
            minNumberOfDistricts += districtType.minNumberOfPlacements;
            maxNumberOfDistricts += districtType.maxNumberOfPlacements;
        }

        districtData.minNumberOfDistricts = minNumberOfDistricts;
        districtData.maxNumberOfDistricts = maxNumberOfDistricts;
    }

    /// <summary>
    /// Generates candidate positions for districts using Poisson disk sampling.
    /// <para>
    /// Validates and sets the candidate points for later district selection.
    /// </para>
    /// </summary>
    private void GenerateCandidatePositions()
    {
        var allPoints = PoissonDiskSampling.GenerateDistrictPoints(
            districtData.numberOfDistricts,
            districtData.minNumberOfDistricts,
            districtData.maxNumberOfDistricts,
            boundariesData.outerBoundaryRadius,
            boundariesData.center,
            poissonData.rejectionSamples);

        districtData.numberOfDistricts = PoissonDiskSampling.ValidateNumberOfDistricts(
            districtData.numberOfDistricts,
            districtData.minNumberOfDistricts,
            districtData.maxNumberOfDistricts);

        poissonData.candidatePoints = allPoints;
    }

    /// <summary>
    /// Applies the generated Voronoi texture.
    /// <para>
    /// Updates the material of the Voronoi diagram with the new texture.
    /// </para>
    /// </summary>
    private void ApplyTexture()
    {
        voronoiData.voronoiTexture.Apply();
        var renderer = voronoiData.voronoiDiagram.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial.mainTexture = voronoiData.voronoiTexture;
        }
    }
    #endregion
}
