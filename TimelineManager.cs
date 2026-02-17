using System;
using System.Collections.Generic;
using System.Linq;

namespace InfernoVEditor
{
    // Represents a single clip on the timeline
    public class TimelineClip
    {
        public Guid Id { get; set; }
        public string SourceFilePath { get; set; }
        public string ClipName { get; set; }

        // Source file times (what portion of the source to use)
        public double SourceStartTime { get; set; }  // In seconds
        public double SourceEndTime { get; set; }

        // Timeline position (where it sits on the timeline)
        public double TimelineStart { get; set; }
        public double TimelineEnd { get; set; }

        // Track assignment
        public int TrackNumber { get; set; }
        public TrackType TrackType { get; set; }

        // Calculated properties
        public double SourceDuration => SourceEndTime - SourceStartTime;
        public double TimelineDuration => TimelineEnd - TimelineStart;

        public TimelineClip()
        {
            Id = Guid.NewGuid();
        }

        // Check if this clip is active at a specific timeline time
        public bool IsActiveAt(double timelineTime)
        {
            return timelineTime >= TimelineStart && timelineTime < TimelineEnd;
        }

        // Get the corresponding source time for a timeline time
        public double GetSourceTime(double timelineTime)
        {
            if (!IsActiveAt(timelineTime))
                return -1;

            double offsetInClip = timelineTime - TimelineStart;
            return SourceStartTime + offsetInClip;
        }
    }

    public enum TrackType
    {
        Video,
        Audio,
        Both
    }

    // Represents a track (V1, V2, A1, A2, etc.)
    public class Track
    {
        public int TrackNumber { get; set; }
        public string TrackName { get; set; }
        public TrackType TrackType { get; set; }
        public bool IsLocked { get; set; }
        public bool IsMuted { get; set; }
        public List<TimelineClip> Clips { get; set; }

        public Track(int trackNumber, TrackType trackType)
        {
            TrackNumber = trackNumber;
            TrackType = trackType;
            TrackName = $"{trackType}{trackNumber}";
            Clips = new List<TimelineClip>();
        }
    }

    // Main Timeline Manager
    public class TimelineManager
    {
        public List<Track> Tracks { get; private set; }
        public double CurrentTime { get; set; }
        public double TotalDuration => CalculateTotalDuration();

        // Events for UI updates
        public event EventHandler<TimelineChangedEventArgs> TimelineChanged;
        public event EventHandler<ClipEventArgs> ClipAdded;
        public event EventHandler<ClipEventArgs> ClipRemoved;
        public event EventHandler<ClipEventArgs> ClipMoved;

        public TimelineManager()
        {
            Tracks = new List<Track>();
            CurrentTime = 0;
        }

        // ===== TRACK MANAGEMENT =====

        public Track AddTrack(TrackType trackType)
        {
            int trackNumber = Tracks.Count(t => t.TrackType == trackType) + 1;
            var track = new Track(trackNumber, trackType);
            Tracks.Add(track);
            OnTimelineChanged();
            return track;
        }

        public void RemoveTrack(int trackNumber)
        {
            var track = Tracks.FirstOrDefault(t => t.TrackNumber == trackNumber);
            if (track != null)
            {
                Tracks.Remove(track);
                OnTimelineChanged();
            }
        }

        public Track GetTrack(int trackNumber, TrackType trackType)
        {
            return Tracks.FirstOrDefault(t =>
                t.TrackNumber == trackNumber && t.TrackType == trackType);
        }

        // ===== CLIP MANAGEMENT =====

        public TimelineClip AddClip(string sourceFilePath, string clipName,
            double sourceStart, double sourceEnd,
            double timelineStart, int trackNumber, TrackType trackType)
        {
            var track = GetTrack(trackNumber, trackType);
            if (track == null)
            {
                track = AddTrack(trackType);
                track.TrackNumber = trackNumber;
            }

            double duration = sourceEnd - sourceStart;

            var clip = new TimelineClip
            {
                SourceFilePath = sourceFilePath,
                ClipName = clipName,
                SourceStartTime = sourceStart,
                SourceEndTime = sourceEnd,
                TimelineStart = timelineStart,
                TimelineEnd = timelineStart + duration,
                TrackNumber = trackNumber,
                TrackType = trackType
            };

            track.Clips.Add(clip);
            OnClipAdded(clip);
            OnTimelineChanged();

            return clip;
        }

        public void RemoveClip(Guid clipId)
        {
            foreach (var track in Tracks)
            {
                var clip = track.Clips.FirstOrDefault(c => c.Id == clipId);
                if (clip != null)
                {
                    track.Clips.Remove(clip);
                    OnClipRemoved(clip);
                    OnTimelineChanged();
                    return;
                }
            }
        }

        public TimelineClip GetClip(Guid clipId)
        {
            foreach (var track in Tracks)
            {
                var clip = track.Clips.FirstOrDefault(c => c.Id == clipId);
                if (clip != null)
                    return clip;
            }
            return null;
        }

        public void MoveClip(Guid clipId, double newTimelineStart, int? newTrackNumber = null)
        {
            var clip = GetClip(clipId);
            if (clip == null)
                return;

            double duration = clip.TimelineDuration;
            clip.TimelineStart = newTimelineStart;
            clip.TimelineEnd = newTimelineStart + duration;

            // Move to different track if specified
            if (newTrackNumber.HasValue && newTrackNumber.Value != clip.TrackNumber)
            {
                var oldTrack = GetTrack(clip.TrackNumber, clip.TrackType);
                var newTrack = GetTrack(newTrackNumber.Value, clip.TrackType);

                if (oldTrack != null && newTrack != null)
                {
                    oldTrack.Clips.Remove(clip);
                    clip.TrackNumber = newTrackNumber.Value;
                    newTrack.Clips.Add(clip);
                }
            }

            OnClipMoved(clip);
            OnTimelineChanged();
        }

        public void TrimClip(Guid clipId, double? newSourceStart = null,
            double? newSourceEnd = null)
        {
            var clip = GetClip(clipId);
            if (clip == null)
                return;

            if (newSourceStart.HasValue)
                clip.SourceStartTime = newSourceStart.Value;

            if (newSourceEnd.HasValue)
                clip.SourceEndTime = newSourceEnd.Value;

            // Update timeline duration
            double newDuration = clip.SourceDuration;
            clip.TimelineEnd = clip.TimelineStart + newDuration;

            OnTimelineChanged();
        }

        // ===== QUERY METHODS =====

        // Get all clips active at a specific time
        public List<TimelineClip> GetActiveClipsAt(double timelineTime)
        {
            var activeClips = new List<TimelineClip>();

            foreach (var track in Tracks)
            {
                foreach (var clip in track.Clips)
                {
                    if (clip.IsActiveAt(timelineTime))
                    {
                        activeClips.Add(clip);
                    }
                }
            }

            return activeClips;
        }

        // Get active video clip at time (highest track takes priority)
        public TimelineClip GetActiveVideoClipAt(double timelineTime)
        {
            var videoTracks = Tracks
                .Where(t => t.TrackType == TrackType.Video || t.TrackType == TrackType.Both)
                .OrderByDescending(t => t.TrackNumber);

            foreach (var track in videoTracks)
            {
                var clip = track.Clips.FirstOrDefault(c => c.IsActiveAt(timelineTime));
                if (clip != null)
                    return clip;
            }

            return null;
        }

        // Get all active audio clips at time (for mixing)
        public List<TimelineClip> GetActiveAudioClipsAt(double timelineTime)
        {
            var audioClips = new List<TimelineClip>();

            var audioTracks = Tracks
                .Where(t => (t.TrackType == TrackType.Audio || t.TrackType == TrackType.Both)
                            && !t.IsMuted);

            foreach (var track in audioTracks)
            {
                foreach (var clip in track.Clips)
                {
                    if (clip.IsActiveAt(timelineTime))
                    {
                        audioClips.Add(clip);
                    }
                }
            }

            return audioClips;
        }

        // Check for overlapping clips on the same track
        public bool HasOverlap(TimelineClip clip)
        {
            var track = GetTrack(clip.TrackNumber, clip.TrackType);
            if (track == null)
                return false;

            return track.Clips.Any(c =>
                c.Id != clip.Id &&
                !(c.TimelineEnd <= clip.TimelineStart || c.TimelineStart >= clip.TimelineEnd)
            );
        }

        // Find nearest clip boundary for snapping
        public double? FindNearestSnapPoint(double timelineTime, double threshold = 0.5)
        {
            var snapPoints = new List<double>();

            foreach (var track in Tracks)
            {
                foreach (var clip in track.Clips)
                {
                    snapPoints.Add(clip.TimelineStart);
                    snapPoints.Add(clip.TimelineEnd);
                }
            }

            var nearest = snapPoints
                .Where(sp => Math.Abs(sp - timelineTime) <= threshold)
                .OrderBy(sp => Math.Abs(sp - timelineTime))
                .FirstOrDefault();

            return nearest != 0 ? (double?)nearest : null;
        }

        // ===== UTILITY METHODS =====

        private double CalculateTotalDuration()
        {
            double maxDuration = 0;

            foreach (var track in Tracks)
            {
                foreach (var clip in track.Clips)
                {
                    if (clip.TimelineEnd > maxDuration)
                        maxDuration = clip.TimelineEnd;
                }
            }

            return maxDuration;
        }

        public void Clear()
        {
            Tracks.Clear();
            CurrentTime = 0;
            OnTimelineChanged();
        }

        // Export timeline data for rendering
        public TimelineExportData GetExportData()
        {
            return new TimelineExportData
            {
                Tracks = Tracks,
                TotalDuration = TotalDuration,
                ExportDate = DateTime.Now
            };
        }

        // ===== EVENT TRIGGERS =====

        private void OnTimelineChanged()
        {
            TimelineChanged?.Invoke(this, new TimelineChangedEventArgs
            {
                TotalDuration = TotalDuration
            });
        }

        private void OnClipAdded(TimelineClip clip)
        {
            ClipAdded?.Invoke(this, new ClipEventArgs { Clip = clip });
        }

        private void OnClipRemoved(TimelineClip clip)
        {
            ClipRemoved?.Invoke(this, new ClipEventArgs { Clip = clip });
        }

        private void OnClipMoved(TimelineClip clip)
        {
            ClipMoved?.Invoke(this, new ClipEventArgs { Clip = clip });
        }
    }

    // ===== EVENT ARGS =====

    public class TimelineChangedEventArgs : EventArgs
    {
        public double TotalDuration { get; set; }
    }

    public class ClipEventArgs : EventArgs
    {
        public TimelineClip Clip { get; set; }
    }

    // Data structure for export
    public class TimelineExportData
    {
        public List<Track> Tracks { get; set; }
        public double TotalDuration { get; set; }
        public DateTime ExportDate { get; set; }
    }
}