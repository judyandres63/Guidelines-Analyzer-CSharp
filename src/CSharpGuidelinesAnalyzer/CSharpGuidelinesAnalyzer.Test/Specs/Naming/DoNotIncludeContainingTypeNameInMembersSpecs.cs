using System;
using CSharpGuidelinesAnalyzer.Naming;
using CSharpGuidelinesAnalyzer.Test.TestDataBuilders;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CSharpGuidelinesAnalyzer.Test.Specs.Naming
{
    public class DoNotIncludeContainingTypeNameInMembersSpecs : CSharpGuidelinesAnalysisTestFixture
    {
        protected override string DiagnosticId => DoNotIncludeContainingTypeNameInMembersAnalyzer.DiagnosticId;

        [Fact]
        public void When_method_name_contains_class_name_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class Employee
                    {
                        static void [|GetEmployee|]()
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Method 'GetEmployee' contains the name of its containing type 'Employee'.");
        }

        [Fact]
        public void When_method_name_does_not_contain_class_name_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class Employee
                    {
                        static void Activate()
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_type_name_consists_of_a_single_letter_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class C
                    {
                        void IsC()
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_field_name_contains_struct_name_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    struct Customer
                    {
                        bool [|IsCustomerActive|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Field 'IsCustomerActive' contains the name of its containing type 'Customer'.");
        }

        [Fact]
        public void When_field_name_does_not_contain_struct_name_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    struct Customer
                    {
                        bool IsActive;
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_property_name_contains_class_name_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class Order
                    {
                        bool [|IsOrderDeleted|] { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Property 'IsOrderDeleted' contains the name of its containing type 'Order'.");
        }

        [Fact]
        public void When_property_name_does_not_contain_class_name_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class Order
                    {
                        bool IsDeleted { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_event_name_contains_class_name_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class Registration
                    {
                        event EventHandler [|RegistrationCompleted|]
                        {
                            add 
                            { 
                                throw new NotImplementedException(); 
                            }
                            remove 
                            { 
                                throw new NotImplementedException(); 
                            }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Event 'RegistrationCompleted' contains the name of its containing type 'Registration'.");
        }

        [Fact]
        public void When_event_name_does_not_contain_class_name_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof (NotImplementedException).Namespace)
                .InGlobalScope(@"
                    class Registration
                    {
                        event EventHandler Completed
                        {
                            add 
                            { 
                                throw new NotImplementedException(); 
                            }
                            remove 
                            { 
                                throw new NotImplementedException(); 
                            }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_enum_member_contains_enum_name_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    enum WindowState
                    {
                        [|WindowStateVisible|]
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Field 'WindowStateVisible' contains the name of its containing type 'WindowState'.");
        }

        [Fact]
        public void When_enum_member_does_not_contain_enum_name_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    enum WindowState
                    {
                        WindowVisible
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_class_contains_constructor_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class Employee
                    {
                        Employee()
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_class_contains_static_constructor_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class Employee
                    {
                        static Employee()
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_class_contains_static_destructor_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class Employee
                    {
                        ~Employee()
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        protected override DiagnosticAnalyzer CreateAnalyzer()
        {
            return new DoNotIncludeContainingTypeNameInMembersAnalyzer();
        }
    }
}