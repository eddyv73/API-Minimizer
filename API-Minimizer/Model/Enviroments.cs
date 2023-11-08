namespace API_Minimizer.Model
{
    public class Enviroments
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string ClusterValue { get; set; }
        public string AppSettingValue { get; set; }
    }


public static class EnviromentsEndpoints
{
	public static void MapEnviromentsEndpoints (this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/Enviroments", () =>
        {
            return new [] { new Enviroments() };
        })
        .WithName("GetAllEnviroments")
        .Produces<Enviroments[]>(StatusCodes.Status200OK);

        routes.MapGet("/api/Enviroments/{id}", (int id) =>
        {
            //return new Enviroments { ID = id };
        })
        .WithName("GetEnviromentsById")
        .Produces<Enviroments>(StatusCodes.Status200OK);

        routes.MapPut("/api/Enviroments/{id}", (int id, Enviroments input) =>
        {
            return Results.NoContent();
        })
        .WithName("UpdateEnviroments")
        .Produces(StatusCodes.Status204NoContent);

        routes.MapPost("/api/Enviroments/", (Enviroments model) =>
        {
            //return Results.Created($"//api/Enviroments/{model.ID}", model);
        })
        .WithName("CreateEnviroments")
        .Produces<Enviroments>(StatusCodes.Status201Created);

        routes.MapDelete("/api/Enviroments/{id}", (int id) =>
        {
            //return Results.Ok(new Enviroments { ID = id });
        })
        .WithName("DeleteEnviroments")
        .Produces<Enviroments>(StatusCodes.Status200OK);
    }
}}
