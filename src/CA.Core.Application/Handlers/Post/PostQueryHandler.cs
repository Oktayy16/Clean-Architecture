﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CA.Core.Application.Contracts.HandlerExchanges.Post.Queries;
using CA.Core.Application.Contracts.Response;
using MediatR;

namespace CA.Core.Application.Handlers.Post
{
    public class PostQueryHandler : IRequestHandler<GetPostByIdQuery, Response<GetPostByIdQueryViewModel>>,
                                    IRequestHandler<GetAllPostQuery, Response<IReadOnlyList<GetAllPostQueryViewModel>>>
    {
        public async Task<Response<GetPostByIdQueryViewModel>> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Response<IReadOnlyList<GetAllPostQueryViewModel>>> Handle(GetAllPostQuery request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
