using ContextSensingClient;
using System;
using System.Windows;

namespace AcerUserSensing
{
    class FeatureCallback : IFeatureCallback
    {
        Logger logger = new Logger();

        public delegate void onFeatureErrorHandler(object sender, FeatureType featureType, Error state);
        public event onFeatureErrorHandler onFeatureError;
        public void OnFeatureErrorOccur(FeatureType featureType, Error state)
        {
            if (onFeatureError != null)
            {
                onFeatureErrorHandler handler = onFeatureError;
                handler(this, featureType, state);
            }
        }

        public delegate void onFeatureEventHandler(object sender, FeatureType featureType, EventType eventType, object eventPayload);
        public event onFeatureEventHandler onFeatureEvent;
        public void OnFeatureEventOccur(FeatureType featureType, EventType eventType, object eventPayload)
        {
            if (onFeatureEvent != null)
            {
                onFeatureEventHandler handler = onFeatureEvent;
                handler(this, featureType, eventType, eventPayload);
            }
        }

        public delegate void onFeatureSuccessHandler(object sender, FeatureType featureType, ResponseType responseType);
        public event onFeatureSuccessHandler onFeatureSuccess;
        public void OnFeatureSuccessOccur(FeatureType featureType, ResponseType responseType)
        {
            if (onFeatureSuccess != null)
            {
                onFeatureSuccessHandler handler = onFeatureSuccess;
                handler(this, featureType, responseType);
            }
        }
        public void OnError(FeatureType featureType, Error state)
        {
            this.OnFeatureErrorOccur(featureType, state);
        }

        public void OnEvent(FeatureType featureType, EventType eventType, object eventPayload)
        {
            this.OnFeatureEventOccur(featureType, eventType, eventPayload);
        }

        public void OnSuccess(FeatureType featureType, ResponseType responseType)
        {
            this.OnFeatureSuccessOccur(featureType, responseType);
        }
    }
}
