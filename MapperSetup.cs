using CompositionalPooling.Utility;
using UnityEngine;


/*
 * Sample setup:
 *		Components covered:
 *			Transform,
 *			Colliders,
 *			Rigidbody,
 *			MeshFilter,
 *			MeshRenderer
 */

namespace CompositionalPooling
{
	public static partial class DelegateManager
	{
		/*
		 * The following is an example of a post mapper delegate.
		 * These delegates are responsible for the mapping operations which require the components and their owning hierarchies to be in a valid and stable state.
		 * Common examples of such operations, are serialization and deserialization of inter-hierarchy references (e.g. a parent referencing one of its children).
		 */
		private static readonly PostMapper _MeshRenderer__PostMapper = (Component source, Component target, ref MappingContext context) =>
		{
			var a = source as MeshRenderer;
			var b = target as MeshRenderer;

			b.lightProbeProxyVolumeOverride = a.lightProbeProxyVolumeOverride.GetCorresponding(ref context); // Resolving probable inter-hierarchy reference.
			b.probeAnchor = a.probeAnchor.GetCorresponding(ref context); // Resolving probable inter-hierarchy reference.
		};


		static DelegateManager() // The static constructors are the best places for doing one-time global initializations.
		{
			/* 
			 * This static constructor registers the base mapper delegates for their corresponding component types.
			 * These delegates are the main mapper delegates which perform self-contained mapping operations independent of the state of the components or their hierarchies.
			 * These delegates may also register the post mapper delegates for performing further mapping operations on the components after their state stablizes.
			 */

			Register(typeof(Transform), (source, target, registeredUnits) =>
			{
				var a = source as Transform;
				var b = target as Transform;

				b.localPosition = a.localPosition;
				b.localRotation = a.localRotation;
				b.localScale = a.localScale;
			});

			Register(typeof(BoxCollider), (source, target, registeredUnits) =>
			{
				var a = source as BoxCollider;
				var b = target as BoxCollider;

				b.center = a.center;
				b.size = a.size;
				b.enabled = a.enabled;
				b.isTrigger = a.isTrigger;
				b.contactOffset = a.contactOffset;
				b.sharedMaterial = a.sharedMaterial;
			});

			Register(typeof(CapsuleCollider), (source, target, registeredUnits) =>
			{
				var a = source as CapsuleCollider;
				var b = target as CapsuleCollider;

				b.center = a.center;
				b.radius = a.radius;
				b.height = a.height;
				b.direction = a.direction;
				b.enabled = a.enabled;
				b.isTrigger = a.isTrigger;
				b.contactOffset = a.contactOffset;
				b.sharedMaterial = a.sharedMaterial;
			});

			Register(typeof(SphereCollider), (source, target, registeredUnits) =>
			{
				var a = source as SphereCollider;
				var b = target as SphereCollider;

				b.center = a.center;
				b.radius = a.radius;
				b.enabled = a.enabled;
				b.isTrigger = a.isTrigger;
				b.contactOffset = a.contactOffset;
				b.sharedMaterial = a.sharedMaterial;
			});

			Register(typeof(MeshCollider), (source, target, registeredUnits) =>
			{
				var a = source as MeshCollider;
				var b = target as MeshCollider;

				b.sharedMesh = a.sharedMesh;
				b.convex = a.convex;
				b.cookingOptions = a.cookingOptions;
				b.enabled = a.enabled;
				b.isTrigger = a.isTrigger;
				b.contactOffset = a.contactOffset;
				b.sharedMaterial = a.sharedMaterial;
			});

			Register(typeof(Rigidbody), (source, target, registeredUnits) =>
			{
				var a = source as Rigidbody;
				var b = target as Rigidbody;

				b.velocity = a.velocity;
				b.angularVelocity = a.angularVelocity;
				b.drag = a.drag;
				b.angularDrag = a.angularDrag;
				b.mass = a.mass;
				b.useGravity = a.useGravity;
				b.maxDepenetrationVelocity = a.maxDepenetrationVelocity;
				b.isKinematic = a.isKinematic;
				b.freezeRotation = a.freezeRotation;
				b.constraints = a.constraints;
				b.collisionDetectionMode = a.collisionDetectionMode;
				b.centerOfMass = a.centerOfMass;
				b.inertiaTensorRotation = a.inertiaTensorRotation;
				b.inertiaTensor = a.inertiaTensor;
				b.detectCollisions = a.detectCollisions;
				b.interpolation = a.interpolation;
				b.solverIterations = a.solverIterations;
				b.sleepThreshold = a.sleepThreshold;
				b.maxAngularVelocity = a.maxAngularVelocity;
				b.solverVelocityIterations = a.solverVelocityIterations;
			});

			Register(typeof(MeshFilter), (source, target, registeredUnits) => (target as MeshFilter).sharedMesh = (source as MeshFilter).sharedMesh);

			Register(typeof(MeshRenderer), (source, target, registeredUnits) =>
			{
				var a = source as MeshRenderer;
				var b = target as MeshRenderer;

				b.additionalVertexStreams = a.additionalVertexStreams;
				b.enlightenVertexStream = a.enlightenVertexStream;
				b.enabled = a.enabled;
				b.shadowCastingMode = a.shadowCastingMode;
				b.receiveShadows = a.receiveShadows;
				b.forceRenderingOff = a.forceRenderingOff;
				b.staticShadowCaster = a.staticShadowCaster;
				b.motionVectorGenerationMode = a.motionVectorGenerationMode;
				b.lightProbeUsage = a.lightProbeUsage;
				b.reflectionProbeUsage = a.reflectionProbeUsage;
				b.renderingLayerMask = a.renderingLayerMask;
				b.rendererPriority = a.rendererPriority;
				b.rayTracingMode = a.rayTracingMode;
				b.sortingLayerName = a.sortingLayerName;
				b.sortingLayerID = a.sortingLayerID;
				b.sortingOrder = a.sortingOrder;
				b.allowOcclusionWhenDynamic = a.allowOcclusionWhenDynamic;
				b.lightmapIndex = a.lightmapIndex;
				b.realtimeLightmapIndex = a.realtimeLightmapIndex;
				b.lightmapScaleOffset = a.lightmapScaleOffset;
				b.realtimeLightmapScaleOffset = a.realtimeLightmapScaleOffset;
				b.sharedMaterial = a.sharedMaterial;
				b.sharedMaterials = a.sharedMaterials.GetCorresponding(b.sharedMaterials);

				registeredUnits.Add(new PostMapperUnit(_MeshRenderer__PostMapper, source, target)); // Registeration of the cached post mapper and its arguments as a post mapping unit.
			});
		}
	}
}