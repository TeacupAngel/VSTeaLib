using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

using HarmonyLib;

namespace TeaLib
{
	namespace HarmonyPatches
	{
		[Flags]
		public enum EnumPatchType
		{
			Server = 1 << 0,
			Client = 1 << 1,
			Common = Server | Client
		}

		[AttributeUsage(AttributeTargets.Class)]  
		public class VSHarmonyPatchAttribute : System.Attribute  
		{  
			public EnumPatchType PatchType {get; private set;}
	
			public VSHarmonyPatchAttribute(EnumPatchType patchType)  
			{  
				PatchType = patchType;
			}  
		}  

		// TODO: Decide if this system should automatically patch everything in every loaded mod, or if mod authors should need to extend it (like with the configs)
		// Or with the new Harmony version, do we even need our own custom attributes? We might be able to patch based on Harmony categories instead
		public class VSHarmonyPatchSystem : ModSystem
		{
			private Harmony harmony;
			private string harmonyId = null;

			private readonly List<VSHarmonyPatchBase> patches = new();

			public override void StartPre(ICoreAPI api)
			{
				if (harmonyId == null)
				{
					StringBuilder stringBuilder = new StringBuilder("vs");
					if (Mod.Info.Authors.Count > 0) stringBuilder.AppendFormat(".{0}", Mod.Info.Authors[0].ToLowerInvariant());
					stringBuilder.AppendFormat(".{0}", Mod.Info.ModID.ToLowerInvariant());
					harmonyId = stringBuilder.ToString();
				}

				harmony = new Harmony(harmonyId);
			}

			public override void StartServerSide(ICoreServerAPI sapi)
			{
				PatchServerSide(harmony, sapi);
			}

			public override void StartClientSide(ICoreClientAPI capi)
			{
				PatchClientSide(harmony, capi);
			}

			public void PatchServerSide(Harmony harmony, ICoreServerAPI sapi)
			{
				foreach(Type type in Assembly.GetExecutingAssembly().GetTypes()) 
				{
					VSHarmonyPatchAttribute patchAttribute = type.GetCustomAttribute<VSHarmonyPatchAttribute>();

					if (typeof(VSHarmonyPatchBase).IsAssignableFrom(type) && (patchAttribute?.PatchType.HasFlag(EnumPatchType.Server) ?? false)) 
					{
						ExecutePatch(type, harmony, sapi);
					}
				}
			}

			public void PatchClientSide(Harmony harmony, ICoreClientAPI capi)
			{
				foreach(Type type in Assembly.GetExecutingAssembly().GetTypes()) 
				{
					VSHarmonyPatchAttribute patchAttribute = type.GetCustomAttribute<VSHarmonyPatchAttribute>();

					if (typeof(VSHarmonyPatchBase).IsAssignableFrom(type) && (patchAttribute?.PatchType.HasFlag(EnumPatchType.Client) ?? false)) 
					{
						// Don't execute common patches on the client in singleplayer, the server already executed them
						if (capi.IsSinglePlayer && patchAttribute.PatchType.HasFlag(EnumPatchType.Server))
						{
							continue;
						}

						ExecutePatch(type, harmony, capi);
					}
				}
			}

			public void ExecutePatch(Type patchType, Harmony harmony, ICoreAPI api)
			{
				VSHarmonyPatchBase patch = Activator.CreateInstance(patchType) as VSHarmonyPatchBase;
				patch.Execute(harmony, api);
				patches.Add(patch);
			}

			public override void Dispose()
			{
				patches.Do<VSHarmonyPatchBase>((VSHarmonyPatchBase patch) => patch.Dispose());
				patches.Clear();

				harmony?.UnpatchAll(harmonyId);
				harmony = null;
			}
		}

		public abstract class VSHarmonyPatchBase
		{
			public abstract void Execute(Harmony harmony, ICoreAPI capi);

			public virtual void Dispose() {}

			protected HarmonyMethod GetPatchMethod(string methodName)
			{
				return new HarmonyMethod(this.GetType().GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
			}
		}
	}
}