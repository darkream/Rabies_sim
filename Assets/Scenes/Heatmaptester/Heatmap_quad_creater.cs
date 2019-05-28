using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Heatmap_quad_creater : MonoBehaviour {
// Use this for initialization
	public Material Matref;
    public OnMapSpawn mapA;
    GameObject quadA;
    Renderer renA;
	
	void Start () {
     	
        quadA= GameObject.CreatePrimitive(PrimitiveType.Quad);
		quadA.transform.position = new Vector3(0, 10, 0);
		quadA.transform.localScale = new Vector3(20, 20, 20);
		quadA.transform.eulerAngles = new Vector3(90.0f,0.0f,0.0f);
	
		renA=quadA.GetComponent<Renderer>();
		renA.material = Matref;
       // createtexture(mapA.xgridsize,mapA.ygridsize);
	}
	
	// Update is called once per frame
	void Update () {

		   if (Input.GetKeyDown("q"))
		   {

			 
			  quadA.transform.localScale = new Vector3(mapA.xgridsize/10.0f, mapA.ygridsize/10.0f, 1);
			    createtexture(mapA.xgridsize,mapA.ygridsize);
		   }
		    if (Input.GetKeyDown("e"))
		   {

			   createtexture2(mapA.xgridsize,mapA.ygridsize);
			  //quadA.transform.localScale = new Vector3(mapA.xgridsize/10.0f, mapA.ygridsize/10.0f, 20);
		   }
		/* 
         if (Input.GetMouseButtonDown(0))
		{createtexture(700,700);}
         if (Input.GetMouseButtonDown(2))
         {quadA.transform.localScale = new Vector3(20, 1, 1);}*/
	}

	private void createtexture(int sizex, int sizey)
    {
        Texture2D texture = new Texture2D(sizex , sizey , TextureFormat.RGB24 , false);
        Color color = new Color(0.0f , 0.0f , 0.0f);
 

        for (int x = 0; x < sizex; x++)
        {
            for (int y = 0; y < sizey; y++)
            {
                if (x<=100)
                {
                   color = new Color(100.0f , 100.0f , 0.0f);
                }
                else if (x<=200)
                {
                   color = new Color(0.0f , 100.0f , 100.0f);
                }
               // color = new Color(0.0f , colorvalue , 0.0f);
                texture.SetPixel(x , y , color);
            }
        }

        texture.Apply();
        renA.material.mainTexture = texture;
/* 
        //encode to png
        byte[] bytes = texture.EncodeToPNG();
        Destroy(texture);

        File.WriteAllBytes(Application.dataPath + "/../Assets/MickRendered/selectedDogTerrainAt" + route +".png" , bytes);*/
    }


	 private void createtexture2(int sizex, int sizey)
    {
        Texture2D texture = new Texture2D(sizex , sizey , TextureFormat.RGB24 , false);
	    Color color2 = new Color(0.0f , 0.0f , 0.0f);
		
        for (int x = 0; x < sizex; x++)
        {
            for (int y = 0; y < sizey; y++)
            {
                if (x%2==0)
                {
					color2=new Color(0.0f,0.0f,0.0f);
					//Debug.Log (x+" "+y);
                }
				else
				{
					color2=new Color(255.0f,0.0f,0.0f);
				}
				 texture.SetPixel(x , y , color2);
              // else texture.SetPixel(x , y , Color.red);
             
                
            }
        }

        texture.Apply();
        renA.material.mainTexture = texture;
		 //Destroy(texture);
/*  
        //encode to png
        byte[] bytes = texture.EncodeToPNG();
        Destroy(texture);

        File.WriteAllBytes(Application.dataPath + "/../Assets/MickRendered/selectedDogTerrainAt" + route +".png" , bytes);*/
    }

	

}
