﻿using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ProductionProfiler.Core.Handlers.Entities;
using ProductionProfiler.Core.Profiling;
using ProductionProfiler.Core.Profiling.Entities;

namespace ProductionProfiler.Core.Binding
{
    public class UpdateProfiledRequestRequestBinder : IUpdateProfiledRequestRequestBinder
    {
        private readonly List<ModelValidationError> _errors = new List<ModelValidationError>();

        public ProfiledRequestUpdate Bind(NameValueCollection formParams)
        {
            var profiledRequest = new ProfiledRequestUpdate
            {
                Delete = formParams.AllKeys.Where(k => k == "Delete").FirstOrDefault() != null,
                Url = formParams.Get("Url")
            };

            if (profiledRequest.Delete)
                return profiledRequest;

            profiledRequest.Server = formParams.Get("Server");
            profiledRequest.ProfilingCount = int.Parse(formParams.Get("ProfilingCount"));
            profiledRequest.Enabled = formParams.Get("Enabled").Contains("true");

            return profiledRequest;
        }

        public bool IsValid(NameValueCollection formParams)
        {
            bool valid = true;
            int profileCount;

            if(!int.TryParse(formParams.Get("ProfilingCount"), out profileCount))
            {
                _errors.Add(new ModelValidationError
                {
                    Field = "ProfilingCount",
                    Message = "ProfileCount was not supplied"
                });

                valid = false;
            }

            if (string.IsNullOrEmpty(formParams.Get("Url")))
            {
                _errors.Add(new ModelValidationError
                {
                    Field = "Url",
                    Message = "Url was not supplied"
                });

                valid = false;
            }

            return valid;
        }

        public List<ModelValidationError> Errors
        {
            get { return _errors; }
        }
    }
}
