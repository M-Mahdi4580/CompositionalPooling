using CompositionalPooling.Utility;
using UnityEngine;


/*
 * Sample setup:
 *		Components covered:
 *			Colliders,
 *			Rigidbody,
 *			MeshFilter,
 *			MeshRenderer
 */

namespace CompositionalPooling
{
	public static partial class StateMapper
	{
		/*
		 * Any serializable field of type GameObject or Component points to an object in some hierarchy and can potentially represent internal hierarchy links (i.e. a reference that can be serialized to a hierarchy path).
		 * Since these fields might represent a hierarchy link, unity's instantiation performs special serialization and deserialization to resolve hierarchy links.
		 * To provide backwards compatibility with unity's instantiation, the main responsiblity of the post mappers, is the resolvation of these links and that, is facilitated by using the provided extension methods.
		 * Post mapper delegates will be called once all base mapping operations are complete and the objects' hierarchies are built. At this point, the objects are in a valid primary state, and any state-dependent mapping can be safely accomplished.
		 * For optimal performance, post mapper delegates should be cached as static fields and registered by the corresponding base mapper when needed. Creating these delegates inside the base mapper itself, creates garbage and degrades performance.
		 */
		private static readonly PostMapper _MeshRenderer__PostMapper = (Component objA, Component objB, ref HierarchyContextInfo contextInfo) =>
		{
			var a = objA as MeshRenderer;
			var b = objB as MeshRenderer;

			b.lightProbeProxyVolumeOverride = a.lightProbeProxyVolumeOverride.transform.GetCorresponding(ref contextInfo).gameObject; // Resolving probable hierarchy link.
			b.probeAnchor = a.probeAnchor.GetCorresponding(ref contextInfo); // Resolving probable hierarchy link.
		};
		
		
		static StateMapper() // In this static constructor, the one-time registerations of the base mappers, are performed.
		{
			/*
			 * The delegates registered below, are the base mapper delegates:
			 *		The system will use these delegates to map the state of the requested object to its corresponding unpooled instance.
			 *		These delegates are responsible for performing the bulk of the object mapping operations. They usually perform bijective assigment of the source component's fields to the target component's fields.
			 *		They are called when the objects' hierarchies are being built and therefore, they can only map the hierarchy independent portions of the components safely.
			 *		Mapping hierarchy-dependent state should only be done after the hierarchies are completely built and this is accomplished by registering a post mapper unit (Comprised of a post mapper delegate alongside its argument data) for the system to call later.
			 *		Most components don't have hierarchy-dependent state and therefore, they won't need any post mapping operations.
			 *		If a component owns unexposed private state (i.e. state that can not be accessed from outside the class), its mapper registeration should be performed in the component's static constructor. This allows access to the component's private state.
			 */

			Register(typeof(BoxCollider), (objA, objB, postMapUnits) =>
			{
				var a = objA as BoxCollider;
				var b = objB as BoxCollider;
				
				b.center = a.center;
				b.size = a.size;
				b.enabled = a.enabled;
				b.isTrigger = a.isTrigger;
				b.contactOffset = a.contactOffset;
				b.sharedMaterial = a.sharedMaterial;
			});
			
			Register(typeof(CapsuleCollider), (objA, objB, postMapUnits) =>
			{
				var a = objA as CapsuleCollider;
				var b = objB as CapsuleCollider;
				
				b.center = a.center;
				b.radius = a.radius;
				b.height = a.height;
				b.direction = a.direction;
				b.enabled = a.enabled;
				b.isTrigger = a.isTrigger;
				b.contactOffset = a.contactOffset;
				b.sharedMaterial = a.sharedMaterial;
			});

			Register(typeof(SphereCollider), (objA, objB, postMapUnits) =>
			{
				var a = objA as SphereCollider;
				var b = objB as SphereCollider;

				b.center = a.center;
				b.radius = a.radius;
				b.enabled = a.enabled;
				b.isTrigger = a.isTrigger;
				b.contactOffset = a.contactOffset;
				b.sharedMaterial = a.sharedMaterial;
			});

			Register(typeof(MeshCollider), (objA, objB, postMapUnits) =>
			{
				var a = objA as MeshCollider;
				var b = objB as MeshCollider;
				
				b.sharedMesh = a.sharedMesh;
				b.convex = a.convex;
				b.cookingOptions = a.cookingOptions;
				b.enabled = a.enabled;
				b.isTrigger = a.isTrigger;
				b.contactOffset = a.contactOffset;
				b.sharedMaterial = a.sharedMaterial;
			});

			Register(typeof(Rigidbody), (objA, objB, postMapUnits) =>
			{
				var a = objA as Rigidbody;
				var b = objB as Rigidbody;

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

			Register(typeof(MeshFilter), (objA, objB, postMapUnits) => (objB as MeshFilter).sharedMesh = (objA as MeshFilter).sharedMesh);
			
			Register(typeof(MeshRenderer), (objA, objB, postMapUnits) =>
			{
				var a = objA as MeshRenderer;
				var b = objB as MeshRenderer;
				
				b.additionalVertexStreams = a.additionalVertexStreams;
				b.enlightenVertexStream = a.enlightenVertexStream;
				b.scaleInLightmap = a.scaleInLightmap;
				b.receiveGI = a.receiveGI;
				b.stitchLightmapSeams = a.stitchLightmapSeams;
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
				
				postMapUnits.Add(new PostMappingUnit(_MeshRenderer__PostMapper, objA, objB)); // Registeration of the cached post mapper and its argument components as post mapping unit.
			});
		}
	}
}
