using System.Collections.Generic;
public class Mobileappinput 
{
    public string ExecName { get; set; }
    public SubdistInfoClass SubdistInfo { get; set; }
    public float NormalDogdata { get; set; }
    //public string InfectDogdata { get; set; }
    public List<InfectDogdata_appinput> InfectDogdata { get; set; }
    public int Grid_size  { get; set; }

    public float Bite_Rate  { get; set; }

    public float Infect_Rate  { get; set; }

    public int Simulation_Day  { get; set; }

    public float Roam_radius  { get; set; }

    public string Current_map_pos_lat  { get; set; }

    public string Current_map_pos_lng  { get; set; }

}
