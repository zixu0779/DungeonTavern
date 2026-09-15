using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Focused import/compile audit; visual regression uses Tools/Demo Flow/Test Wall Cutout.
public static class WallSectionCheck
{
    [MenuItem("Tools/Demo Flow/Prepare Wall Sections")]
    public static void Prepare()
    {
        foreach(var name in new[]{"WallSectionBrick", "WallSectionStone"})
        {
            var path="Assets/DungeonTavern/Art/Environment/Architecture/Walls/Sections/Resources/"+name+".png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(!importer) throw new System.Exception("Missing section texture: "+path);
            importer.textureType=TextureImporterType.Default;
            importer.sRGBTexture=true;importer.wrapMode=TextureWrapMode.Repeat;
            importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=true;
            importer.maxTextureSize=1024;importer.SaveAndReimport();
        }
        var report=new StringBuilder();
        foreach(var name in new[]{"DungeonTavern/Wall Cutout Unlit","DungeonTavern/Native Pixel Face"})
        {
            var shader=Shader.Find(name);report.AppendLine(name);
            foreach(var message in ShaderUtil.GetShaderMessages(shader))
                report.AppendLine($"{message.severity}: {message.file}:{message.line} {message.message} {message.messageDetails}");
        }
        File.WriteAllText("/tmp/wall-section-shaders.txt",report.ToString());
    }
}
