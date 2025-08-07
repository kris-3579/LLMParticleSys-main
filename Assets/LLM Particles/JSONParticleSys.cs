using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// requires particle system to be attached to the same GameObject
[RequireComponent(typeof(ParticleSystem))]
public class JSONParticleSys : MonoBehaviour
{

    ParticleSystem ps = null;

    public void LoadAndRun(string configFilePath)
    {
        // get a particle system to the GameObject
        ps = gameObject.GetComponent<ParticleSystem>();
        
        // Stop the system
        ps.Stop();

        // Load the configuration from the JSON file
        string jsonString = System.IO.File.ReadAllText(configFilePath);
        var config = JObject.Parse(jsonString);
        

        // Set the parameters of the particle system
        SetMainModuleParameters((JObject)config["MainModule"]);
        SetColorOverLifetimeModule((JObject)config["ColorOverLifetimeModule"]);
        SetShapeModule((JObject)config["ShapeModule"]);
        SetVelocityOverLifetimeModule((JObject)config["VelocityOverLifetimeModule"]);
        setEmissionModule((JObject)config["EmissionModule"]);
        setNoiseModule((JObject)config["NoiseModule"]);
        setSizeOverLifetimeModule((JObject)config["SizeOverLifetimeModule"]);
        //setRotationOverLifetimeModule((JObject)config["RotationOverLifetimeModule"]);
        //setSubEmittersModule((JObject)config["SubEmittersModule"]);
        //setCollisionModule((JObject)config["CollisionModule"]);
        //setTrailsModule((JObject)config["TrailsModule"]);
        // Play the system
        ps.Play();

    }

    private static ParticleSystem.MinMaxCurve ParseMinMax(JProperty minMaxStr)
    {
        try
        {
            // try to parse the string as a float
            float minMaxFloat;
            minMaxFloat = (float)minMaxStr.Value;
            return minMaxFloat;
        }
        catch (Exception)
        {
            // if it fails, try to parse it as a object with keys "min" and "max"
            var minMaxDict = minMaxStr.Value;
            float min = (float)minMaxDict["min"];
            float max = (float)minMaxDict["max"];
            return new ParticleSystem.MinMaxCurve(min, max);
        }
    }

    public void SetMainModuleParameters(JObject parameters)
    {
        var mainModule = ps.main;

        foreach (var param in parameters.Properties())
        {
            switch (param.Name)
            {
                case "loop":
                    mainModule.loop = (bool)param.Value;
                    break;
                case "duration":
                    mainModule.duration = (float)param.Value;
                    break;
                case "startColor":
                    // cast a dictionary<string, float> to a color, where the keys are "r", "g", "b", "a"
                    var colorDict = param.Value;
                    
                    mainModule.startColor = new Color((float)colorDict["r"], (float)colorDict["g"], (float)colorDict["b"], (float)colorDict["a"]);
                    break;
                case "startSize":

                    // check if the value is a dictionary with keys "x", "y", "z"
                    if (param.Value.Type == JTokenType.Object && (param.Value["x"] != null && param.Value["y"] != null && param.Value["z"] != null))
                    {
                        JToken sizeDict = param.Value;
                        // if it does, parse it as a Vector
                        mainModule.startSizeX = (float)sizeDict["x"];
                        mainModule.startSizeY = (float)sizeDict["y"];
                        mainModule.startSizeZ = (float)sizeDict["z"];
                    }
                    else
                    {
                        // otherwise, parse it as a MinMaxCurve
                        mainModule.startSize = ParseMinMax(param);
                    }
                    break;
                case "startSpeed":
                    mainModule.startSpeed = ParseMinMax(param);
                    break;
                case "startDelay":
                    mainModule.startDelay = ParseMinMax(param);
                    break;
                case "startLifetime":
                    mainModule.startLifetime = ParseMinMax(param);
                    break;
                case "playOnAwake":
                    mainModule.playOnAwake = (bool)param.Value;
                    break;
                case "gravityModifier":
                    mainModule.gravityModifier = ParseMinMax(param);
                    break;
                case "simulationSpace":
                    // cast the string to the enum type
                    mainModule.simulationSpace = (ParticleSystemSimulationSpace)Enum.Parse(typeof(ParticleSystemSimulationSpace), (string)param.Value);
                    break;
                case "scalingMode":
                    // cast the string to the enum type
                    mainModule.scalingMode = (ParticleSystemScalingMode)Enum.Parse(typeof(ParticleSystemScalingMode), (string)param.Value);
                    break;
                case "maxParticles":
                    mainModule.maxParticles = (int)param.Value;
                    break;
                case "emitterVelocityMode":
                    // cast the string to the enum type
                    mainModule.emitterVelocityMode = (ParticleSystemEmitterVelocityMode)Enum.Parse(typeof(ParticleSystemEmitterVelocityMode), (string)param.Value);
                    break;
                case "emitterVelocity":
                    var ev = param.Value;
                    mainModule.emitterVelocity = new Vector3((float)ev["x"], (float)ev["y"], (float)ev["z"]);
                    break;

                default:
                    // try to set the parameter using reflection
                    Type type = mainModule.GetType();
                    PropertyInfo prop = type.GetProperty(param.Name);
                    if (prop != null)
                    {
                        try
                        {
                            prop.SetValue(mainModule, ParseMinMax(param), null);
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                prop.SetValue(mainModule, (bool)param.Value, null);
                            }
                            catch (Exception e2)
                            {
                                Debug.Log($"Error setting parameter {param.Name}: {e.Message} {e2.Message}");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"Parameter {param.Name} not found in ParticleSystem.MainModule");
                    }
                    break;
            }
        }
    }

    public void SetShapeModule(JObject param)
    {
        var shapeModule = ps.shape;
        foreach (var p in param.Properties())
        {
            switch (p.Name)
            {
                case "shapeType":
                    // cast the string to the enum type
                    shapeModule.shapeType = (ParticleSystemShapeType)Enum.Parse(typeof(ParticleSystemShapeType), (string)p.Value);
                    break;
                case "radius":
                    shapeModule.radius = (float)p.Value;
                    break;
                case "radiusThickness":
                    shapeModule.radiusThickness = (float)p.Value;
                    break;
                case "angle":
                    shapeModule.angle = (float)p.Value;
                    break;
                case "length":
                    shapeModule.length = (float)p.Value;
                    break;
                case "arc":
                    shapeModule.arc = (float)p.Value;
                    break;
                case "arcMode":
                    // check if the value is within the enum values
                    if (!Enum.IsDefined(typeof(ParticleSystemShapeMultiModeValue), (string)p.Value))
                    {
                        Debug.Log($"Invalid value for arcMode: {p.Value}");
                        break;
                    }
                    // cast the string to the enum type
                    shapeModule.arcMode = (ParticleSystemShapeMultiModeValue)Enum.Parse(typeof(ParticleSystemShapeMultiModeValue), (string)p.Value);
                    break;
                case "arcSpread":
                    shapeModule.arcSpread = (float)p.Value;
                    break;
                case "arcSpeed":
                    shapeModule.arcSpeed = (float)p.Value;
                    break;
                case "arcSpeedMultiplier":
                    shapeModule.arcSpeedMultiplier = (float)p.Value;
                    break;
                // for donut shape
                case "donutRadius":
                    shapeModule.donutRadius = (float)p.Value;
                    break;
                // for box shape
                case "boxThickness":
                    // cast a dictionary<string, float> to a Vector3, where the keys are "x", "y", "z"
                    var boxThicknessDict = p.Value;
                    shapeModule.boxThickness = new Vector3((float)boxThicknessDict["x"], (float)boxThicknessDict["y"], (float)boxThicknessDict["z"]);
                    break;
                case "position":
                    // cast a dictionary<string, float> to a Vector3, where the keys are "x", "y", "z"
                    var positionDict = p.Value;
                    shapeModule.position = new Vector3((float)positionDict["x"], (float)positionDict["y"], (float)positionDict["z"]);
                    break;
                case "rotation":
                    var rotationDict = p.Value;
                    shapeModule.rotation = new Vector3((float)rotationDict["x"], (float)rotationDict["y"], (float)rotationDict["z"]);
                    break;
                case "scale":
                    var scaleDict = p.Value;
                    shapeModule.scale = new Vector3((float)scaleDict["x"], (float)scaleDict["y"], (float)scaleDict["z"]);
                    break;
                case "alignToDirection":
                    shapeModule.alignToDirection = (bool)p.Value;
                    break;
                case "randomDirectionAmount":
                    shapeModule.randomDirectionAmount = (float)p.Value;
                    break;
                case "sphericalDirectionAmount":
                    shapeModule.sphericalDirectionAmount = (float)p.Value;
                    break;
                case "randomPositionAmount":
                    shapeModule.randomPositionAmount = (float)p.Value;
                    break;

            }
        }
    }

    public void SetColorOverLifetimeModule(JObject parameters)
    {
        /* input example:
         * {
              "color_key": [
                {"color": {"r": 0, "g": 0, "b": 255}, "time": 0},    // Mystic Blue at the beginning
                {"color": {"r": 128, "g": 0, "b": 128}, "time": 0.5},// Transition to Brilliant Purple in the middle
                {"color": {"r": 255, "g": 255, "b": 255}, "time": 1} // Fade to White (Mystical Glow) at the end
              ],
              "alpha_key": [
                {"value": 1, "time": 0},    // Fully opaque at the beginning
                {"value": 0.5, "time": 0.5},// Half transparent in the middle
                {"value": 0, "time": 1}     // Fully transparent at the end
              ]
            }
        */
        // skip if parameters is null or empty
        if (parameters == null || !parameters.HasValues)
        {
            return;
        }

        var colorOverTimeModule = ps.colorOverLifetime;

        // enable the color over lifetime module
        colorOverTimeModule.enabled = true;

        // create lists to store the color and alpha keys
        var colorKeys = new List<GradientColorKey>();
        var alphaKeys = new List<GradientAlphaKey>();
        foreach (var p in parameters.Properties())
        {
            switch (p.Name)
            {
                case "color_key":
                    var colorKeyList = (JArray)p.Value;
                    foreach (var colorKey in colorKeyList)
                    {
                        var color = colorKey["color"];
                        colorKeys.Add(new GradientColorKey(new Color((float)color["r"], (float)color["g"], (float)color["b"]), (float)colorKey["time"]));
                    }
                    break;
                case "alpha_key":
                    var alphaKeyList = (JArray)p.Value;
                    foreach (var alphaKey in alphaKeyList)
                    {
                        alphaKeys.Add(new GradientAlphaKey((float)alphaKey["value"], (float)alphaKey["time"]));
                    }
                    break;
            }
        }
        var gradient = new Gradient();
        gradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
        colorOverTimeModule.color = gradient;
    }

    private static ParticleSystem.MinMaxCurve ParseMinMaxCurve(JProperty minMaxStr)
    {
        /* input example 1:
         * {
                "type": "curve",
                "keyPoints": [[0, 0.2], [1, 1]]
           }
         * input example 2:
           {
                "type": "constant",
                "value": 5
           }
         */
        var minMaxCurve = new ParticleSystem.MinMaxCurve();
        var minMaxCurveDict = (JObject)minMaxStr.Value;
        var type = (string)minMaxCurveDict["type"];
        switch (type)
        {
            case "curve":
                var keyPoints = (JArray)minMaxCurveDict["keyPoints"];
                var curve = new AnimationCurve();
                foreach (var keyPoint in keyPoints)
                {
                    curve.AddKey((float)keyPoint[0], (float)keyPoint[1]);
                }
                minMaxCurve.curve = curve;
                break;
            case "constant":
                minMaxCurve.constant = (float)minMaxCurveDict["value"];
                break;
        }
        return minMaxCurve;
    }

    public void SetVelocityOverLifetimeModule(JObject parameters)
    {
        /* input example:
         * {
              "radial": {
                "type": "curve",
                "keyPoints": [[0, 0.2], [1, 1]]
              },
              "orbitalX": {
                "type": "curve",
                "keyPoints": [[0, 0.5], [1, 0]]
              },
              "orbitalY": {
                "type": "curve",
                "keyPoints": [[0, 0.5], [1, 0]]
              },
              "orbitalZ": {
                "type": "constant",
                "value": 5
              }
            }
         */
        // skip if parameters is null or empty
        if (parameters == null || !parameters.HasValues)
        {
            return;
        }

        var velocityOverLifetimeModule = ps.velocityOverLifetime;

        // enable the velocity over lifetime module
        velocityOverLifetimeModule.enabled = true;

        foreach (var p in parameters.Properties())
        {
            switch (p.Name)
            {
                case "radial":
                    velocityOverLifetimeModule.radial = ParseMinMaxCurve(p);
                    break;
                case "orbitalX":
                    velocityOverLifetimeModule.orbitalX = ParseMinMaxCurve(p);
                    break;
                case "orbitalY":
                    velocityOverLifetimeModule.orbitalY = ParseMinMaxCurve(p);
                    break;
                case "orbitalZ":
                    velocityOverLifetimeModule.orbitalZ = ParseMinMaxCurve(p);
                    break;
                case "x":
                    velocityOverLifetimeModule.x = ParseMinMaxCurve(p);
                    break;
                case "y":
                    velocityOverLifetimeModule.y = ParseMinMaxCurve(p);
                    break;
                case "z":
                    velocityOverLifetimeModule.z = ParseMinMaxCurve(p);
                    break;

            }
        }
    }

    public void setEmissionModule(JObject parameters) {
     
        if (parameters == null || !parameters.HasValues)
        {
            return;
        }

        var emission = ps.emission;
        emission.enabled = true;

        
        foreach(var p in parameters.Properties())
        {
            switch (p.Name)
            {
                case "rateOverTime":
                    emission.rateOverTime = (float)p.Value;
                    break;

                case "rateOverDistance":
                    emission.rateOverDistance = (float)p.Value;
                    break;

                case "rateOverDistanceMultiplier":
                    emission.rateOverDistanceMultiplier = (float) p.Value;
                    break;

                case "rateOverTimeMultiplier":
                    emission.rateOverTimeMultiplier = (float) p.Value;     
                    
                    break;


                case "bursts":
                    var bursts = (JArray)p.Value;

                    //Debug.Log(bursts);
                    List<ParticleSystem.Burst> burstsList = new List<ParticleSystem.Burst>();
                    foreach(var val in bursts){
        
                                burstsList.Add(new ParticleSystem.Burst((float) val["Time"],(short) val["Count"],(short) val["Cycles"], (int) val["Interval"], (float) val["Probability"]));
                        }
                    //new ParticleStstem.Burst(p["count"],p["cycleCount"],p["maxCount"], p["minCount"], p["probability"], p["repeatInterval"], p["time"])
                    //new ParticleStstem.Burst(p["Time"],p["Count"],p["Cycles"], p["minCount"], p["Interval"], p["Probability"]); 
                    emission.SetBursts(burstsList.ToArray());

                    break;
                        
            }

        }  

    }


    public void setNoiseModule(JObject parameters){

        if (parameters == null || !parameters.HasValues)
        {
            return;
        }
        var noise = ps.noise;
        noise.enabled = true;


      
        
        foreach (var p in parameters.Properties()) {
            switch (p.Name)
                {
                    //Control if the particles move up and down or side to side on an axis
                    // bool
                    case "separateAxes":
                        noise.separateAxes = (bool)p.Value; 
                        break;

                    // controls the direction where the particles move
                    //float
                    case "strength":
                        noise.strength = (float)p.Value;
                        break;

                    // low values for smoother movement of particles
                    // float 
                    case "frequency":
                        noise.frequency = (float) p.Value;
                        break;

                    // higher values for more unpredictable movements
                    // float
                    case "scrollSpeed":
                        noise.scrollSpeed = (float)p.Value;
                        break;

                    // When enabled, strength is proportional to frequency. Tying these values together means the noise field can be scaled while maintaining the same behaviour, but at a different size.
                    // bool
                    case "damping":
                        noise.damping = (bool) p.Value;
                        break;
                    // non configurable 
                    
                    
                    case "octaveCount":
                        noise.octaveCount = (int) p.Value; //1
                        break; 

                    case "octaveMultiplier":
                        noise.octaveMultiplier = (float) p.Value; //1
                        break;

                    case "octaveScale":
                        noise.octaveScale = (float) p.Value; //1
                        break;
                    
                    case "quality":
                        string val = (string) p.Value;
                        if (string.Equals(val, "Low")){
                            noise.quality =  ParticleSystemNoiseQuality.Low;
                        } 
                        else if (string.Equals(val, "Medium")){
                            noise.quality =  ParticleSystemNoiseQuality.Medium;
                        } 
                        else if (string.Equals(val, "High")){
                            noise.quality =  ParticleSystemNoiseQuality.High;
                        } 
                        break;

                    
                    // cofigure later
                    case "remap":
                        //noise.remap = new ParticleSystem.MinMaxCurve(val["size"], val["curve"]); // true
                    break; 
                        
                    // configures how fast the particles move
                    // float
                    case "positionAmount":
                        noise.positionAmount =(float)p.Value;
                        break;


                    case "rotationAmount":
                        noise.rotationAmount = (float) p.Value;
                        break;

                    case "sizeAmount":
                        noise.sizeAmount = (float) p.Value;
                        break;


                    
                }

        }
             
    } 


    public void setSizeOverLifetimeModule(JObject parameters) {

        if (parameters == null || !parameters.HasValues)
        {
            return;
        }
        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        
        
        float size = 1;
        AnimationCurve curve = new AnimationCurve();

        if (parameters == null | !parameters.HasValues){
            return;
        }

        foreach (var p in parameters.Properties()) {
            switch (p.Name)
                { 
                     case "size":

                        size = (float) p.Value;

                        break;

                    case "curve":
                        var curvesList = (JArray)p.Value;
                        
                        foreach (var curveKey in curvesList)
                        {
                            curve.AddKey((float)curveKey["min"], (float)curveKey["max"]);
                        }

                        break;


                    case "separateAxes":

                    sz.separateAxes = (bool) p.Value; 
                    break;

                    
                }
    } 

    sz.size = new ParticleSystem.MinMaxCurve(size, curve);

    /* AnimationCurve curve = new AnimationCurve();
        curve.AddKey(2.0f, 0.1f);
        curve.AddKey(0.75f, 0.5f);

    sz.size = new ParticleSystem.MinMaxCurve(10f, curve);  */
 
    
    //Debug.Log("yes");
    }

    public void setRotationOverLifetimeModule(JObject parameters)
    {
        if (parameters == null | !parameters.HasValues){
            return;
        }
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;


        float constant = 1;
        AnimationCurve curve1 = new AnimationCurve();
         AnimationCurve curve2 = new AnimationCurve();

        

        foreach (var p in parameters.Properties()) {
            switch (p.Name)
                { 
                    case "constant":
                    constant = (float)p.Value;

                    break;

                    case "curve1":
                        var curvesList1 = (JArray)p.Value;
                        foreach (var curveKey in curvesList1)
                        {
                            curve1.AddKey((float)curveKey["min"], (float)curveKey["max"]);
                        }

                        break;

                    case "curve2":
                        var curvesList2 = (JArray)p.Value;
                        foreach (var curveKey in curvesList2)
                        {
                            curve2.AddKey((float)curveKey["min"], (float)curveKey["max"]);
                        }

                        break;




                    //case: "SeparateAxes":
                }
    } 

    rot.z = new ParticleSystem.MinMaxCurve(constant, curve1, curve2);
    }

    public void setSubEmittersModule(JObject parameters) {
        if (parameters == null | !parameters.HasValues){
            return;
        }
        // Set up the sub-emitter.

        var subEmittersGO = new GameObject("Particle System");
        var subParticleSystem = subEmittersGO.AddComponent<ParticleSystem>();

        subEmittersGO.transform.SetParent(ps.transform);
        var subEmittersModule = ps.subEmitters;
        subEmittersModule.enabled = true;


        foreach (var p in parameters.Properties()) {
            switch (p.Name)
                { 
                    case "add":
                    
                        var subEmitter = (JArray)p.Value;
                        ParticleSystemSubEmitterType type = ParticleSystemSubEmitterType.Death;
                        ParticleSystemSubEmitterProperties property = ParticleSystemSubEmitterProperties.InheritNothing;

                        foreach (var sub in subEmitter)
                        {
                            if (string.Equals(sub["type"], "Birth")){
                                type = ParticleSystemSubEmitterType.Birth;
                            }
                            if (string.Equals(sub["type"], "Death")){
                                type = ParticleSystemSubEmitterType.Death;
                            }


                            if (string.Equals(sub["property"], "InheritNothing")){
                                property = ParticleSystemSubEmitterProperties.InheritNothing;
                            }
                            if (string.Equals(sub["property"], "InheritEverything")){
                                property = ParticleSystemSubEmitterProperties.InheritEverything;
                            }
                            if (string.Equals(sub["property"], "InheritColor")){
                                property = ParticleSystemSubEmitterProperties.InheritColor;
                            }
                            if (string.Equals(sub["property"], "InheritSize")){
                                property = ParticleSystemSubEmitterProperties.InheritSize;
                            }
                            if (string.Equals(sub["property"], "InheritRotation")){
                                property = ParticleSystemSubEmitterProperties.InheritRotation;
                            }
                            if (string.Equals(sub["property"], "InheritLifetime")){
                                property = ParticleSystemSubEmitterProperties.InheritLifetime;
                            }
                            


                            subEmittersModule.AddSubEmitter(subParticleSystem, type, property);
                        }

                        break;




                }
        } 
    }

    public void setCollisionModule(JObject parameters) {

        if (parameters == null | !parameters.HasValues){
            return;
        }
        // Set up the sub-emitter.

        var coll = ps.collision;
        coll.enabled = true;


        foreach (var p in parameters.Properties()) {
            switch (p.Name)
                { 

                
                    case "bounce": // ParticleSystem.MinMaxCurve

                        coll.bounce = (float)p.Value;

                        break;

                    case "bounceMultiplier":

                        coll.bounceMultiplier = (float)p.Value;

                        break;

                    case "colliderForce":

                        coll.colliderForce = (float)p.Value;

                        break;

                    // TODO https://docs.unity3d.com/ScriptReference/ParticleSystem.CollisionModule-collidesWith.html
                    /* case "collidesWith":

                        coll.collidesWith;

                        break; */

                    case "dampen": //ParticleSystem.MinMaxCurve

                        coll.dampen = (float)p.Value;

                        break;

                    case "dampenMultiplier":

                        coll.dampenMultiplier = (float)p.Value;

                        break;

                    case "enableDynamicColliders":

                        coll.enableDynamicColliders = (bool) p.Value;

                        break;

                    case "lifetimeLoss": // ParticleSystem.MinMaxCurve

                        coll.lifetimeLoss = (float) p.Value;

                        break;

                    case "lifetimeLossMultiplier":

                        coll.lifetimeLossMultiplier = (float) p.Value;

                        break;

                    case "maxCollisionShapes":

                        coll.maxCollisionShapes = (int) p.Value;

                        break;

                    case "maxKillSpeed":

                        coll.maxKillSpeed = (float) p.Value;

                        break;

                    case "minKillSpeed":
                        coll.minKillSpeed = (float) p.Value;

                        break;

                    case "mode":
                        if (string.Equals("2D", (string)p.Value)){

                            coll.mode = ParticleSystemCollisionMode.Collision2D;

                        } else if (string.Equals("3D", (string)p.Value)) {

                            coll.mode = ParticleSystemCollisionMode.Collision3D;


                        }
                        break;

                    
                    case "multiplyColliderForceByCollisionAngle":
                        coll.multiplyColliderForceByCollisionAngle = (bool) p.Value;

                        break;

                    case "multiplyColliderForceByParticleSize":
                        coll.multiplyColliderForceByParticleSize = (bool) p.Value;

                        break;

                    case "multiplyColliderForceByParticleSpeed":
                        coll.multiplyColliderForceByParticleSpeed = (bool) p.Value;

                        break;

                    /* case "planeCount": // readOnly - nonconfingurable
                        coll.planeCount;
                        break; */

                    // TODO
                    /* case "quality ": // ParticleSystemCollisionQuality
                        coll.quality;
                        break; */

                    case "radiusScale":
                        coll.radiusScale = (float) p.Value;
                        break;

                    case "sendCollisionMessages":
                        coll.sendCollisionMessages = (bool) p.Value;
                        break;

                    //TODO
                    /* case "type": //ParticleSystemCollisionType 
                        coll.type;
                        break; */

                    case "voxelSize":
                        coll.voxelSize = (float) p.Value;
                        break;

                    


                    
                    

                    

                    

                   

                    

                }
        } 
    }

     /* public void setTrailsModule(JObject parameters) {
        if (parameters == null | !parameters.HasValues){
            return;
        }
        // Set up the sub-emitter.

        var trails = ps.trails;
        trails.enabled = true;



        foreach (var p in parameters.Properties()) {
            switch (p.Name)
                { 
                    case "mode": 
                        
                        var mode = Particle;
                        if(string.Equals((string) p.Value, "Particles")){

                            mode = mode.Particle;
                        }

                        if(string.Equals((string) p.Value, "Ribbon")){
                            mode = mode.Ribbon;

                        }

                        trails.mode = mode;

                        break;  
                    
                    case "ratio":

                        
                        trails.ratio = (float)p.Value;

                        break;
                        

                    case "lifetime":

                        trails.lifetime = (float)p.Value;

                        break; 

                     case "minimumVertexDistance":

                        trails.minimumVertexDistance = (float)p.Value;

                        break; 

                    case "worldSpace":

                        trails.worldSpace = (bool)p.Value;

                        break;

                    case "dieWithParticles":

                        trails.dieWithParticles = (bool)p.Value;

                        break; 

                    case "ribbonCount":

                        trails.ribbonCount = (int) p.Value;

                        break; 

                    case "splitSubEmitterRibbons":

                        trails.splitSubEmitterRibbons= (bool)p.Value;

                        break; 

                    case "attachRibbonsToTransform":

                        trails.attachRibbonsToTransform = (bool)p.Value;
     
                        break; 

                    case "textureMode":

                        trails.textureMode = p.value;
                        break; 

                    case "textureScale":

                        trails.textureScale.X = (float) p.value[X];
                        trails.textureScale.Y = (float) p.value[Y];
                       
                    break;

                    case "sizeAffectsWidth":

                        trails.sizeAffectsWidth = (bool)p.value;
                        break;

                    case "inheritParticleColor": 


                        trails.inheritParticleColor = (bool) p.value;
                        break;

                    // I dont know how to configure this
                    case "ColorOverLifetime":

                        
                        break; 

                    

                    case "widthOverTrail": 

                        trails.widthOverTrail = (int)p.value;
                        break;


                    // I don't know how to configure this
                    case "colorOverTrail": 


                        
                        break; 

                    case "generateLightingData": // high medium or low


                        trails.generateLightingData = (bool)p.value;
                        break;

                    case "shadowBias": // high medium or low


                        trails.shadowBias = (float)p.value;
                        break;
                   
  

                }
        } 
    } */
}
