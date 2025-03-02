﻿using Quiz.Site.Models;
using Quiz.Site.Services;
using Umbraco.Cms.Core.Composing;

namespace Quiz.Site.Composing
{
    public class RegisterServicesComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddTransient<IAccountService, AccountService>();
            builder.Services.AddTransient<IDataTypeValueService, DataTypeValueService>();
            builder.Services.AddTransient<IMediaUploadService, MediaUploadService>();
            builder.Services.AddTransient<IQuestionRepository, QuestionRepository>();
            builder.Services.AddTransient<IQuizResultRepository, QuizResultRepository>();
            builder.Services.AddTransient<IQuestionService, QuestionService>();
            builder.Services.AddTransient<IQuizResultService, QuizResultService>();
            builder.Services.AddTransient<IBadgeService, BadgeService>();
            builder.Services.AddTransient<INotificationRepository, NotificationRepository>();
            builder.Services.AddTransient<IReadNotificationRepository, ReadNotificationRepository>();
            builder.Services.AddTransient<IhCaptchaService, hCaptchaService>();
        }
    }
}
