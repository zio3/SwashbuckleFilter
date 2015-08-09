using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Swashbuckle.Swagger;

namespace Swashbuckle.Swagger
{
    public static class SchemaHelper
    {
        static public void Apply(Schema schema, SchemaRegistry schemaRegistry, System.Type type)
        {
            var workStr = type.ToString();

            if (type.IsEnum)
            {
                EnumRegist(schemaRegistry, type);
            }
            if (IsNullableEnumType(type))
            {
                var innerType = type.GetGenericArguments()[0];
                EnumRegist(schemaRegistry, innerType);
            }


            if (schema.type == "array")
            {
                if (type.IsArray)
                {
                    Apply(schema.items, schemaRegistry, type.GetElementType());
                }
                else
                {
                    Apply(schema.items, schemaRegistry, type.GetGenericArguments()[0]);
                }
                return;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                schema.vendorExtensions.Add("CsType", type.GetGenericArguments()[0].Name + "?");
                schema.vendorExtensions.Add("CsNamespace", type.Namespace);

            }
            else
            {
                schema.vendorExtensions.Add("CsType", type.Name);
                schema.vendorExtensions.Add("CsNamespace", type.Namespace);
            }

            if (schema.properties != null)
            {
                foreach (var prop in schema.properties)
                {
                    var pi = type.GetProperty(prop.Key);
                    if (pi != null)
                    {
                        Apply(prop.Value, schemaRegistry, pi.PropertyType);
                    }
                }
            }
        }

        static private bool IsNullableEnumType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return type.GetGenericArguments()[0].IsEnum;
            }
            return false;
        }

        public static void EnumRegist(SchemaRegistry schemaRegistry, System.Type type)
        {
            if (!schemaRegistry.Definitions.ContainsKey(type.Name))
            {
                var enumSchema = new Schema();
                enumSchema.type = "enum";
                enumSchema.vendorExtensions.Add("Names", Enum.GetNames(type));
                var nums = Enum.GetValues(type).Cast<Object>().Select(c => (int)c);
                enumSchema.vendorExtensions.Add("Values", nums);


                enumSchema.vendorExtensions.Add("CsType", type.Name);
                enumSchema.vendorExtensions.Add("CsNamespace", type.Namespace);

                schemaRegistry.Definitions.Add(type.Name, enumSchema);
            }
        }

    }

    public class CsSchemaFilter : ISchemaFilter
    {
        public void Apply(Schema schema, SchemaRegistry schemaRegistry, System.Type type)
        {
            SchemaHelper.Apply(schema, schemaRegistry, type);

        }



    }

    public class CsOperationFilter : IOperationFilter
    {

        public void Apply(Operation operation, SchemaRegistry schemaRegistry, System.Web.Http.Description.ApiDescription apiDescription)
        {
            if (operation.parameters != null)
            {
                foreach (var param in operation.parameters)
                {
                    var paramBinding = apiDescription.ParameterDescriptions.FirstOrDefault(c => c.ParameterDescriptor.ParameterName == param.name);
                    var type = paramBinding.ParameterDescriptor.ParameterType;

                    if (param.schema != null && param.schema.type == "array")
                    {
                        SchemaHelper.Apply(param.schema, schemaRegistry, type);
                        continue;
                    }

                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        param.vendorExtensions.Add("CsType", type.GetGenericArguments()[0].Name + "?");
                        param.vendorExtensions.Add("CsNamespace", type.Namespace);
                        if (type.GetGenericArguments()[0].IsEnum)
                        {
                            SchemaHelper.EnumRegist(schemaRegistry, type.GetGenericArguments()[0]);
                        }
                    }
                    else
                    {
                        param.vendorExtensions.Add("CsType", type.Name);
                        param.vendorExtensions.Add("CsNamespace", type.Namespace);
                        if (type.IsEnum)
                        {
                            SchemaHelper.EnumRegist(schemaRegistry, type);
                        }
                    }
                }
            }
            foreach (var responce in operation.responses)
            {
                var type = apiDescription.ActionDescriptor.ReturnType;

                if (type == null)
                    continue;

                if (responce.Value.schema.type == "array")
                    continue;


                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    responce.Value.schema.vendorExtensions.Add("CsType", type.GetGenericArguments()[0].Name + "?");
                    responce.Value.schema.vendorExtensions.Add("CsNamespace", type.Namespace);
                    if (type.GetGenericArguments()[0].IsEnum)
                    {
                        SchemaHelper.EnumRegist(schemaRegistry, type.GetGenericArguments()[0]);
                    }
                }
                else
                {
                    responce.Value.schema.vendorExtensions.Add("CsType", type.Name);
                    responce.Value.schema.vendorExtensions.Add("CsNamespace", type.Namespace);
                    if (type.IsEnum)
                    {
                        SchemaHelper.EnumRegist(schemaRegistry, type);
                    }
                }
            }
        }
    }
}
