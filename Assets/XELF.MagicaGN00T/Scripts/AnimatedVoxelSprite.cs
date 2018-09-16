using UnityEngine;
namespace GN00T.MagicaUnity {
	/// <summary>
	/// An animated voxel sprite
	/// </summary>
	public class AnimatedVoxelSprite : VoxelSprite {
		private float _elapsed = 0f;
		private int currentFrame = 0;
		public VoxelAnimation _animation = null;

		void Update() {
			if (_animation != null) {
				_elapsed += Time.deltaTime;
				var _animScale = Mathf.Clamp(_elapsed / _animation.runTime, 0, 1);
				var frame = (int)(_animScale * (_animation.endFrame - _animation.startFrame)) + _animation.startFrame;
				if (currentFrame != frame)
					_meshFilter.mesh = model.meshes[frame].LODs[0].opaque;
			}
		}

		public bool CurrentAnimationCompleted =>
			_animation == null || _elapsed > _animation.runTime;

		public void SetAnimation(VoxelAnimation animation) {
			_animation = animation;
			if (_animation != null) {
				model = animation.targetData;
				_elapsed = 0;
				currentFrame = 0;
				_meshFilter.sharedMesh = model.meshes[animation.startFrame].LODs[0].opaque;
			}
		}
	}
}
